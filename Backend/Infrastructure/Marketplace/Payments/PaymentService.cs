using System.Text.Json;
using Application.DTOs.Marketplace.Ledger;
using Application.DTOs.Marketplace.Payments;
using Application.DTOs.Marketplace.Recommendations;
using Application.Interfaces.Marketplace.Ledger;
using Application.Interfaces.Marketplace.Orders;
using Application.Interfaces.Marketplace.Payments;
using Application.Interfaces.Marketplace.Recommendations;
using Application.Interfaces.Repositories;
using Domain.Entities.Marketplace.Payments;
using Domain.Enums;
using Infrastructure.External.Realtime;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Marketplace.Payments;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICheckoutService _checkoutService;
    private readonly ILedgerService _ledgerService;
    private readonly IEnumerable<IPaymentGateway> _gateways;
    private readonly IHubContext<SocialHub> _hubContext;
    private readonly IProductEventChannel? _productEventChannel;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IUnitOfWork unitOfWork,
        ICheckoutService checkoutService,
        ILedgerService ledgerService,
        IEnumerable<IPaymentGateway> gateways,
        IHubContext<SocialHub> hubContext,
        ILogger<PaymentService> logger,
        IProductEventChannel? productEventChannel = null)
    {
        _unitOfWork = unitOfWork;
        _checkoutService = checkoutService;
        _ledgerService = ledgerService;
        _gateways = gateways;
        _hubContext = hubContext;
        _logger = logger;
        _productEventChannel = productEventChannel;
    }

    public async Task<PaymentLinkDto> InitiatePaymentAsync(
        Guid userId,
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var checkout = await _unitOfWork.Checkouts.GetByIdAsync(request.CheckoutId, cancellationToken);
        if (checkout == null || checkout.BuyerUserId != userId)
        {
            throw new KeyNotFoundException("Không tìm thấy phiên thanh toán hợp lệ.");
        }

        // Nếu đơn hàng đã thanh toán thành công (Paid), trả về trạng thái Succeeded ngay thay vì ném ngoại lệ
        if (checkout.Status == CheckoutStatus.Paid)
        {
            var paidPayment = await _unitOfWork.Payments.GetByCheckoutIdAsync(checkout.Id, cancellationToken);
            var (accNum, accName, desc, code) = ParseAccountPayload(paidPayment?.RawPayloadRef, $"Don {checkout.CheckoutNo}", paidPayment?.ProviderTxnId ?? string.Empty);

            return new PaymentLinkDto
            {
                PaymentId = paidPayment?.Id ?? Guid.Empty,
                CheckoutId = checkout.Id,
                Provider = paidPayment?.Provider ?? request.Provider,
                ProviderTxnId = paidPayment?.ProviderTxnId ?? string.Empty,
                Amount = paidPayment?.Amount ?? checkout.GrandTotal,
                Currency = paidPayment?.Currency ?? "VND",
                QrCodeUrl = paidPayment?.QrCodeUrl,
                PaymentUrl = paidPayment?.PaymentUrl,
                PaymentCode = code,
                Description = desc,
                AccountNumber = accNum,
                AccountName = accName,
                Status = PaymentStatus.Succeeded,
                CreatedAt = paidPayment?.CreatedAtUtc ?? checkout.CreatedAtUtc
            };
        }

        if (checkout.Status != CheckoutStatus.Open)
        {
            throw new InvalidOperationException($"Phiên thanh toán không ở trạng thái mở (Trạng thái hiện tại: {checkout.Status}).");
        }

        if (checkout.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Phiên thanh toán đã hết hạn 15 phút. Vui lòng tạo phiên thanh toán mới.");
        }

        // Nếu đã có giao dịch Initiated trước đó của cùng cổng thì trả lại link hiện có
        var existingPayment = await _unitOfWork.Payments.GetByCheckoutIdAsync(checkout.Id, cancellationToken);
        if (existingPayment != null && existingPayment.Status == PaymentStatus.Initiated && existingPayment.Provider == request.Provider)
        {
            var (accNum, accName, desc, code) = ParseAccountPayload(existingPayment.RawPayloadRef, $"Don {checkout.CheckoutNo}", existingPayment.ProviderTxnId);

            return new PaymentLinkDto
            {
                PaymentId = existingPayment.Id,
                CheckoutId = checkout.Id,
                Provider = existingPayment.Provider,
                ProviderTxnId = existingPayment.ProviderTxnId,
                Amount = existingPayment.Amount,
                Currency = existingPayment.Currency,
                QrCodeUrl = existingPayment.QrCodeUrl,
                PaymentUrl = existingPayment.PaymentUrl,
                PaymentCode = code,
                Description = desc,
                AccountNumber = accNum,
                AccountName = accName,
                Status = existingPayment.Status,
                CreatedAt = existingPayment.CreatedAtUtc
            };
        }

        var gateway = _gateways.FirstOrDefault(g => g.Provider == request.Provider)
            ?? _gateways.FirstOrDefault(g => g.Provider == PaymentProvider.Mock)
            ?? throw new InvalidOperationException($"Không tìm thấy cổng thanh toán phù hợp cho {request.Provider}.");

        var description = $"Thanh toan don {checkout.CheckoutNo}";
        var linkResult = await gateway.CreatePaymentLinkAsync(
            checkout.Id,
            checkout.CheckoutNo,
            checkout.GrandTotal,
            description,
            cancellationToken);

        var accountPayload = BuildAccountPayload(linkResult.AccountNumber, linkResult.AccountName, linkResult.Description ?? description, linkResult.PaymentCode ?? linkResult.ProviderTxnId);

        var payment = new Payment(
            checkout.Id,
            request.Provider,
            linkResult.ProviderTxnId,
            checkout.GrandTotal,
            "QR",
            "VND",
            linkResult.QrCodeUrl,
            linkResult.PaymentUrl,
            accountPayload);

        await _unitOfWork.Payments.AddAsync(payment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PaymentLinkDto
        {
            PaymentId = payment.Id,
            CheckoutId = checkout.Id,
            Provider = payment.Provider,
            ProviderTxnId = payment.ProviderTxnId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            QrCodeUrl = payment.QrCodeUrl,
            PaymentUrl = payment.PaymentUrl,
            PaymentCode = linkResult.PaymentCode ?? payment.ProviderTxnId,
            Description = linkResult.Description ?? description,
            AccountNumber = linkResult.AccountNumber,
            AccountName = linkResult.AccountName,
            Status = payment.Status,
            CreatedAt = payment.CreatedAtUtc
        };
    }

    public async Task<PaymentDetailDto?> GetPaymentByCheckoutIdAsync(
        Guid userId,
        Guid checkoutId,
        CancellationToken cancellationToken = default)
    {
        var checkout = await _unitOfWork.Checkouts.GetByIdAsync(checkoutId, cancellationToken);
        if (checkout == null || checkout.BuyerUserId != userId) return null;

        var payment = await _unitOfWork.Payments.GetByCheckoutIdAsync(checkoutId, cancellationToken);
        if (payment == null) return null;

        var isSucceeded = checkout.Status == CheckoutStatus.Paid || payment.Status == PaymentStatus.Succeeded;
        var status = isSucceeded ? PaymentStatus.Succeeded : payment.Status;
        var (accNum, accName, desc, code) = ParseAccountPayload(payment.RawPayloadRef, $"Don {checkout.CheckoutNo}", payment.ProviderTxnId);

        return new PaymentDetailDto
        {
            Id = payment.Id,
            CheckoutId = payment.CheckoutId,
            Provider = payment.Provider,
            ProviderTxnId = payment.ProviderTxnId,
            Method = payment.Method,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = status,
            PaidAt = payment.PaidAt ?? (isSucceeded ? checkout.PaidAtUtc ?? DateTime.UtcNow : null),
            QrCodeUrl = payment.QrCodeUrl,
            PaymentUrl = payment.PaymentUrl,
            PaymentCode = code,
            Description = desc,
            AccountNumber = accNum,
            AccountName = accName,
            CreatedAt = payment.CreatedAtUtc
        };
    }

    public async Task<WebhookProcessResult> HandleWebhookAsync(
        PaymentProvider provider,
        string webhookBody,
        string signature,
        CancellationToken cancellationToken = default)
    {
        var gateway = _gateways.FirstOrDefault(g => g.Provider == provider);
        if (gateway == null)
        {
            return new WebhookProcessResult { Success = false, Message = $"Cổng thanh toán {provider} không được hỗ trợ." };
        }

        if (!gateway.VerifyWebhookSignature(webhookBody, signature))
        {
            _logger.LogWarning("Webhook chữ ký không hợp lệ từ {Provider}.", provider);
            return new WebhookProcessResult { Success = false, Message = "Chữ ký webhook không hợp lệ." };
        }

        var providerTxnId = gateway.ParseWebhookTransactionId(webhookBody);
        if (string.IsNullOrWhiteSpace(providerTxnId))
        {
            return new WebhookProcessResult { Success = false, Message = "Không tìm thấy mã giao dịch trong webhook." };
        }

        var payment = await _unitOfWork.Payments.GetByProviderTxnIdAsync(provider, providerTxnId, cancellationToken);
        if (payment == null)
        {
            _logger.LogInformation("Webhook chữ ký hợp lệ từ {Provider} nhưng không tìm thấy Payment {TxnId} (Webhook kiểm tra/xác thực liên kết từ cổng).", provider, providerTxnId);
            return new WebhookProcessResult { Success = true, Message = "Webhook hợp lệ (kiểm tra liên kết cổng thành công)." };
        }

        if (payment.Status == PaymentStatus.Succeeded)
        {
            _logger.LogInformation("Giao dịch {TxnId} đã được xử lý thành công trước đó (Idempotent).", providerTxnId);
            return new WebhookProcessResult { Success = true, Message = "Giao dịch đã được ghi nhận trước đó.", PaymentId = payment.Id, CheckoutId = payment.CheckoutId };
        }

        var success = await ExecutePaymentSuccessSettlementAsync(payment, webhookBody[..Math.Min(webhookBody.Length, 500)], cancellationToken);
        if (!success)
        {
            return new WebhookProcessResult { Success = false, Message = "Lỗi xử lý hoàn tất thanh toán." };
        }

        return new WebhookProcessResult
        {
            Success = true,
            Message = "Xử lý webhook thành công.",
            PaymentId = payment.Id,
            CheckoutId = payment.CheckoutId
        };
    }

    public async Task<PaymentDetailDto> VerifyAndSyncPaymentAsync(
        Guid userId,
        Guid checkoutId,
        CancellationToken cancellationToken = default)
    {
        var checkout = await _unitOfWork.Checkouts.GetByIdAsync(checkoutId, cancellationToken);
        if (checkout == null || checkout.BuyerUserId != userId)
        {
            throw new KeyNotFoundException("Không tìm thấy phiên thanh toán.");
        }

        var payment = await _unitOfWork.Payments.GetByCheckoutIdAsync(checkoutId, cancellationToken);
        if (payment == null)
        {
            throw new InvalidOperationException("Chưa khởi tạo giao dịch thanh toán cho phiên này.");
        }

        if (checkout.Status == CheckoutStatus.Paid || payment.Status == PaymentStatus.Succeeded)
        {
            if (payment.Status != PaymentStatus.Succeeded)
            {
                payment.MarkSucceeded(checkout.PaidAtUtc ?? DateTime.UtcNow, "Checkout already paid");
                _unitOfWork.Payments.Update(payment);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var (accN, accNm, dsc, cd) = ParseAccountPayload(payment.RawPayloadRef, $"Don {checkout.CheckoutNo}", payment.ProviderTxnId);
            return new PaymentDetailDto
            {
                Id = payment.Id,
                CheckoutId = payment.CheckoutId,
                Provider = payment.Provider,
                ProviderTxnId = payment.ProviderTxnId,
                Method = payment.Method,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = PaymentStatus.Succeeded,
                PaidAt = payment.PaidAt ?? checkout.PaidAtUtc ?? DateTime.UtcNow,
                QrCodeUrl = payment.QrCodeUrl,
                PaymentUrl = payment.PaymentUrl,
                PaymentCode = cd,
                Description = dsc,
                AccountNumber = accN,
                AccountName = accNm,
                CreatedAt = payment.CreatedAtUtc
            };
        }

        var gateway = _gateways.FirstOrDefault(g => g.Provider == payment.Provider)
            ?? _gateways.FirstOrDefault(g => g.Provider == PaymentProvider.Mock);

        if (gateway != null)
        {
            var verifyResult = await gateway.VerifyPaymentAsync(payment.ProviderTxnId, cancellationToken);
            if (verifyResult.IsPaid)
            {
                _logger.LogInformation("Verification Fallback xác nhận giao dịch {TxnId} đã thanh toán.", payment.ProviderTxnId);
                await ExecutePaymentSuccessSettlementAsync(payment, $"Verification Fallback: {verifyResult.Status}", cancellationToken);
            }
        }

        var updated = await _unitOfWork.Payments.GetByIdAsync(payment.Id, cancellationToken) ?? payment;
        var (accNum, accName, desc, code) = ParseAccountPayload(updated.RawPayloadRef, $"Don {checkout.CheckoutNo}", updated.ProviderTxnId);

        return new PaymentDetailDto
        {
            Id = updated.Id,
            CheckoutId = updated.CheckoutId,
            Provider = updated.Provider,
            ProviderTxnId = updated.ProviderTxnId,
            Method = updated.Method,
            Amount = updated.Amount,
            Currency = updated.Currency,
            Status = updated.Status,
            PaidAt = updated.PaidAt,
            QrCodeUrl = updated.QrCodeUrl,
            PaymentUrl = updated.PaymentUrl,
            PaymentCode = code,
            Description = desc,
            AccountNumber = accNum,
            AccountName = accName,
            CreatedAt = updated.CreatedAtUtc
        };
    }

    private static (string? AccountNumber, string? AccountName, string? Description, string? PaymentCode) ParseAccountPayload(
        string? rawPayloadRef,
        string defaultDesc,
        string defaultCode)
    {
        if (string.IsNullOrWhiteSpace(rawPayloadRef) || !rawPayloadRef.StartsWith('{'))
        {
            return (null, null, defaultDesc, defaultCode);
        }

        try
        {
            using var doc = JsonDocument.Parse(rawPayloadRef);
            var root = doc.RootElement;
            var accNum = root.TryGetProperty("accountNumber", out var an) ? an.GetString() : null;
            var accName = root.TryGetProperty("accountName", out var anm) ? anm.GetString() : null;
            var desc = root.TryGetProperty("description", out var d) ? d.GetString() : defaultDesc;
            var code = root.TryGetProperty("paymentCode", out var c) ? c.GetString() : defaultCode;
            return (accNum, accName, desc, code);
        }
        catch
        {
            return (null, null, defaultDesc, defaultCode);
        }
    }

    private static string BuildAccountPayload(string? accountNumber, string? accountName, string? description, string? paymentCode)
    {
        return JsonSerializer.Serialize(new
        {
            accountNumber,
            accountName,
            description,
            paymentCode
        });
    }

    private async Task<bool> ExecutePaymentSuccessSettlementAsync(
        Payment payment,
        string? payloadRef,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        // GIAO DỊCH A: Đổi trạng thái Succeeded nguyên tử & Nhận tiền vào sổ cái (GatewayClearing -> UnallocatedPayments)
        var claimed = await _unitOfWork.Payments.TryMarkSucceededAsync(payment.Id, nowUtc, payloadRef, cancellationToken);
        if (!claimed)
        {
            await _unitOfWork.Payments.ReloadAsync(payment, cancellationToken);
            _logger.LogInformation("Payment {PaymentId} đã được xử lý Succeeded bởi một luồng khác.", payment.Id);
        }
        else
        {
            await _unitOfWork.Payments.ReloadAsync(payment, cancellationToken);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var gatewayClearing = await _unitOfWork.Ledgers.GetOrCreateSystemAccountAsync(LedgerAccountType.GatewayClearing, cancellationToken);
                var unallocated = await _unitOfWork.Ledgers.GetOrCreateSystemAccountAsync(LedgerAccountType.UnallocatedPayments, cancellationToken);

                var ledgerLinesA = new List<LedgerPostingLine>
                {
                    new() { AccountId = gatewayClearing.Id, Debit = payment.Amount, Credit = 0 },
                    new() { AccountId = unallocated.Id, Debit = 0, Credit = payment.Amount }
                };

                await _ledgerService.PostTransactionAsync(
                    LedgerTransactionType.PaymentReceived,
                    "Payment",
                    payment.Id,
                    $"pay_recv:{payment.Id}",
                    $"Nhận tiền thanh toán giao dịch {payment.ProviderTxnId}",
                    ledgerLinesA,
                    cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Lỗi khi ghi sổ nhận tiền cho Payment {PaymentId}", payment.Id);
                return false;
            }
        }

        // GIAO DỊCH B: Hoàn tất Checkout, giao hàng, sinh ShopOrders và phân bổ sổ cái
        try
        {
            var checkoutResult = await _checkoutService.CompleteCheckoutPaymentAsync(payment.CheckoutId, payment.ProviderTxnId, cancellationToken);

            if (checkoutResult.RefundRequired)
            {
                _logger.LogWarning("Phiên Checkout {CheckoutId} thanh toán sau khi hết hạn và không còn mã, chuyển sang chờ hoàn tiền (RefundPayable).", payment.CheckoutId);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    var unallocated = await _unitOfWork.Ledgers.GetOrCreateSystemAccountAsync(LedgerAccountType.UnallocatedPayments, cancellationToken);
                    var refundPayable = await _unitOfWork.Ledgers.GetOrCreateSystemAccountAsync(LedgerAccountType.RefundPayable, cancellationToken);

                    var refundLines = new List<LedgerPostingLine>
                    {
                        new() { AccountId = unallocated.Id, Debit = payment.Amount, Credit = 0 },
                        new() { AccountId = refundPayable.Id, Debit = 0, Credit = payment.Amount }
                    };

                    await _ledgerService.PostTransactionAsync(
                        LedgerTransactionType.RefundProcessed,
                        "Checkout",
                        payment.CheckoutId,
                        $"late_pay_refund:{payment.Id}",
                        $"Chuyển tiền thanh toán muộn phiên {payment.CheckoutId} sang chờ hoàn tiền",
                        refundLines,
                        cancellationToken);

                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _logger.LogError(ex, "Lỗi khi hạch toán RefundPayable cho thanh toán muộn Payment {PaymentId}", payment.Id);
                    return false;
                }

                return true;
            }

            await _ledgerService.SettleCheckoutPaymentAsync(payment.CheckoutId, payment, cancellationToken);

            // Bắn ProductEventType.Purchase vào In-Memory Channel cho từng sản phẩm trong Checkout (Non-blocking)
            if (_productEventChannel != null)
            {
                try
                {
                    var checkoutEntity = await _unitOfWork.Checkouts.GetByIdAsync(payment.CheckoutId, cancellationToken);
                    if (checkoutEntity?.Items != null)
                    {
                        foreach (var item in checkoutEntity.Items)
                        {
                            await _productEventChannel.EnqueueEventAsync(new ProductEventMessage
                            {
                                UserId = checkoutEntity.BuyerUserId,
                                ProductId = item.ProductId,
                                EventType = ProductEventType.Purchase,
                                DwellTimeSeconds = null,
                                CreatedAtUtc = DateTime.UtcNow
                            }, cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi khi bắn sự kiện Purchase vào Recommendation Channel cho Checkout {CheckoutId}", payment.CheckoutId);
                }
            }

            // Bắn tín hiệu WebSocket Realtime tới màn hình Checkout đang mở của khách hàng
            try
            {
                await _hubContext.Clients.Group($"checkout_{payment.CheckoutId}").SendAsync("PaymentSucceeded", new
                {
                    checkoutId = payment.CheckoutId,
                    paymentId = payment.Id,
                    amount = payment.Amount,
                    status = "Succeeded",
                    paidAt = DateTime.UtcNow
                }, cancellationToken);

                _logger.LogInformation("Đã phát SignalR PaymentSucceeded tới group checkout_{CheckoutId}", payment.CheckoutId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi phát SignalR PaymentSucceeded cho Checkout {CheckoutId}", payment.CheckoutId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi hoàn tất thanh toán Checkout {CheckoutId}", payment.CheckoutId);
            return false;
        }
    }
}
