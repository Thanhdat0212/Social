using Domain.Entities.Marketplace.Orders;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Tests.Marketplace;

public class CheckoutTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldInitializeOpenCheckout()
    {
        // Arrange
        var buyerId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        const long itemsTotal = 250_000;
        const long grandTotal = 250_000;

        // Act
        var checkout = new Checkout(
            checkoutNo: "CK261029-ABC123",
            buyerUserId: buyerId,
            itemsTotal: itemsTotal,
            discountTotal: 0,
            grandTotal: grandTotal,
            expiresAtUtc: expiresAt,
            idempotencyKey: "idem-key-1");

        // Assert
        checkout.Status.Should().Be(CheckoutStatus.Open);
        checkout.CheckoutNo.Should().Be("CK261029-ABC123");
        checkout.BuyerUserId.Should().Be(buyerId);
        checkout.GrandTotal.Should().Be(grandTotal);
        checkout.ExpiresAtUtc.Should().Be(expiresAt);
        checkout.PaidAtUtc.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidCheckoutNo_ShouldThrowArgumentException(string invalidNo)
    {
        // Act & Assert
        var act = () => new Checkout(
            invalidNo,
            Guid.NewGuid(),
            100_000,
            0,
            100_000,
            DateTime.UtcNow.AddMinutes(15));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*CheckoutNo*");
    }

    [Fact]
    public void Constructor_WithZeroOrNegativeGrandTotal_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => new Checkout(
            "CK-TEST-1",
            Guid.NewGuid(),
            0,
            0,
            0, // invalid total
            DateTime.UtcNow.AddMinutes(15));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*lớn hơn 0*");
    }

    [Fact]
    public void MarkPaid_WhenOpen_ShouldTransitionToPaidAndSetPaidAtUtc()
    {
        // Arrange
        var checkout = new Checkout(
            "CK-TEST-PAID",
            Guid.NewGuid(),
            150_000,
            0,
            150_000,
            DateTime.UtcNow.AddMinutes(15));

        // Act
        checkout.MarkPaid();

        // Assert
        checkout.Status.Should().Be(CheckoutStatus.Paid);
        checkout.PaidAtUtc.Should().NotBeNull();
        checkout.PaidAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void MarkPaid_WhenAlreadyPaid_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var checkout = new Checkout(
            "CK-TEST-ALREADY-PAID",
            Guid.NewGuid(),
            150_000,
            0,
            150_000,
            DateTime.UtcNow.AddMinutes(15));
        checkout.MarkPaid();

        // Act & Assert
        var act = () => checkout.MarkPaid();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể đánh dấu thanh toán*");
    }

    [Fact]
    public void MarkExpired_WhenOpen_ShouldTransitionToExpired()
    {
        // Arrange
        var checkout = new Checkout(
            "CK-TEST-EXP",
            Guid.NewGuid(),
            100_000,
            0,
            100_000,
            DateTime.UtcNow.AddMinutes(-1));

        // Act
        checkout.MarkExpired();

        // Assert
        checkout.Status.Should().Be(CheckoutStatus.Expired);
    }

    [Fact]
    public void MarkExpired_WhenAlreadyPaid_ShouldNotChangeStatus()
    {
        // Arrange
        var checkout = new Checkout(
            "CK-TEST-PAID-NOT-EXP",
            Guid.NewGuid(),
            100_000,
            0,
            100_000,
            DateTime.UtcNow.AddMinutes(-1));
        checkout.MarkPaid();

        // Act
        checkout.MarkExpired();

        // Assert: Không thể chuyển từ Paid sang Expired
        checkout.Status.Should().Be(CheckoutStatus.Paid);
    }

    [Fact]
    public void CheckoutItem_CommissionAndNetCalculation_ShouldBeCorrect()
    {
        // Arrange
        const long unitPrice = 200_000;
        const int quantity = 2;
        const long lineTotal = unitPrice * quantity; // 400,000
        const int commissionRateBp = 1000; // 10%
        const long expectedCommission = 40_000;
        const long expectedShopNet = 360_000;

        // Act
        var item = new CheckoutItem(
            checkoutId: Guid.NewGuid(),
            shopId: Guid.NewGuid(),
            variantId: Guid.NewGuid(),
            productId: Guid.NewGuid(),
            categoryId: Guid.NewGuid(),
            quantity: quantity,
            unitPrice: unitPrice,
            lineTotal: lineTotal,
            productNameSnapshot: "Tài khoản Canva Pro",
            variantLabelSnapshot: "1 Năm",
            skuSnapshot: "CANVA-PRO-1Y",
            commissionRateBp: commissionRateBp,
            commissionAmount: expectedCommission,
            shopNet: expectedShopNet,
            deliveryType: DeliveryType.CodePool);

        // Assert
        item.LineTotal.Should().Be(lineTotal);
        item.CommissionAmount.Should().Be(expectedCommission);
        item.ShopNet.Should().Be(expectedShopNet);
        (item.ShopNet + item.CommissionAmount).Should().Be(item.LineTotal);
    }
}
