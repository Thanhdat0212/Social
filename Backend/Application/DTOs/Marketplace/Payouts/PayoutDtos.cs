using Domain.Enums;

namespace Application.DTOs.Marketplace.Payouts;

public class CreateShopBankAccountRequest
{
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
}

public class ShopBankAccountDto
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumberMasked { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public BankAccountStatus Status { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CreatePayoutRequest
{
    public long Amount { get; set; }
    public string? RequestIdempotencyKey { get; set; }
}

public class PayoutHistoryDto
{
    public Guid Id { get; set; }
    public PayoutStatus PreviousStatus { get; set; }
    public PayoutStatus NewStatus { get; set; }
    public PayoutActorType ActorType { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class PayoutDto
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string? ShopName { get; set; }
    public long Amount { get; set; }
    public long Fee { get; set; }
    public long NetAmount { get; set; }
    public PayoutStatus Status { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumberMasked { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string? BankTransactionRef { get; set; }
    public string? FailureReason { get; set; }
    public string? AdminNote { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public List<PayoutHistoryDto> StatusHistories { get; set; } = new();
}

public class AdminApprovePayoutRequest
{
    public string? AdminNote { get; set; }
}

public class AdminRejectPayoutRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class AdminMarkPaidPayoutRequest
{
    public string BankTransactionRef { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class AdminMarkUnknownPayoutRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class ShopCancelPayoutRequest
{
    public string Reason { get; set; } = string.Empty;
}
