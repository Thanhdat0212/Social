using Application.Common;
using Application.DTOs.Marketplace.Ledger;
using Application.Interfaces.Repositories;
using Application.Interfaces.Repositories.Marketplace.Ledger;
using Domain.Entities.Marketplace.Ledger;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Marketplace.Ledger;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests.Marketplace;

public class LedgerServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<ILedgerRepository> _mockLedgerRepo;
    private readonly Mock<ICacheService> _mockCache;
    private readonly LedgerService _ledgerService;

    public LedgerServiceTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockLedgerRepo = new Mock<ILedgerRepository>();
        _mockCache = new Mock<ICacheService>();

        _mockUow.Setup(u => u.Ledgers).Returns(_mockLedgerRepo.Object);

        _ledgerService = new LedgerService(
            _mockUow.Object,
            _mockCache.Object,
            NullLogger<LedgerService>.Instance);
    }

    [Fact]
    public async Task PostTransactionAsync_WhenDebitDoesNotEqualCredit_ShouldThrowInvalidOperationException()
    {
        // Arrange: Tổng nợ 100k, tổng có 90k (lệch sổ)
        var lines = new List<LedgerPostingLine>
        {
            new() { AccountId = Guid.NewGuid(), Debit = 100_000, Credit = 0 },
            new() { AccountId = Guid.NewGuid(), Debit = 0, Credit = 90_000 }
        };

        // Act
        var act = async () => await _ledgerService.PostTransactionAsync(
            LedgerTransactionType.OrderSettlement,
            "ShopOrder",
            Guid.NewGuid(),
            "tx-unbalanced-1",
            "Test unbalanced",
            lines);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*không cân*");
    }

    [Fact]
    public async Task PostTransactionAsync_WhenTotalDebitIsZeroOrNegative_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var lines = new List<LedgerPostingLine>
        {
            new() { AccountId = Guid.NewGuid(), Debit = 0, Credit = 0 },
            new() { AccountId = Guid.NewGuid(), Debit = 0, Credit = 0 }
        };

        // Act
        var act = async () => await _ledgerService.PostTransactionAsync(
            LedgerTransactionType.OrderSettlement,
            "ShopOrder",
            Guid.NewGuid(),
            "tx-zero-1",
            "Test zero amount",
            lines);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*phải lớn hơn 0*");
    }

    [Fact]
    public async Task PostTransactionAsync_WhenLinesCountLessThanTwo_ShouldThrowArgumentException()
    {
        // Arrange: Chỉ có 1 bút toán đơn lẻ
        var lines = new List<LedgerPostingLine>
        {
            new() { AccountId = Guid.NewGuid(), Debit = 100_000, Credit = 0 }
        };

        // Act
        var act = async () => await _ledgerService.PostTransactionAsync(
            LedgerTransactionType.OrderSettlement,
            "ShopOrder",
            Guid.NewGuid(),
            "tx-single-line-1",
            "Single line",
            lines);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*ít nhất 2 bút toán*");
    }

    [Fact]
    public async Task PostTransactionAsync_WhenTransactionAlreadyExistsWithIdempotencyKey_ShouldSkipWithoutAddingAgain()
    {
        // Arrange
        const string idempotencyKey = "tx-idempotent-key-123";
        _mockLedgerRepo.Setup(r => r.TransactionExistsAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var lines = new List<LedgerPostingLine>
        {
            new() { AccountId = Guid.NewGuid(), Debit = 50_000, Credit = 0 },
            new() { AccountId = Guid.NewGuid(), Debit = 0, Credit = 50_000 }
        };

        // Act
        await _ledgerService.PostTransactionAsync(
            LedgerTransactionType.OrderSettlement,
            "ShopOrder",
            Guid.NewGuid(),
            idempotencyKey,
            "Memo",
            lines);

        // Assert: Không thêm transaction mới và không gọi SaveChanges
        _mockLedgerRepo.Verify(r => r.AddTransactionAsync(It.IsAny<LedgerTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PostTransactionAsync_WhenBalancedAndNew_ShouldAddTransactionAndSave()
    {
        // Arrange
        const string idempotencyKey = "tx-valid-new-key-456";
        _mockLedgerRepo.Setup(r => r.TransactionExistsAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var lines = new List<LedgerPostingLine>
        {
            new() { AccountId = Guid.NewGuid(), Debit = 200_000, Credit = 0 },
            new() { AccountId = Guid.NewGuid(), Debit = 0, Credit = 200_000 }
        };

        // Act
        await _ledgerService.PostTransactionAsync(
            LedgerTransactionType.OrderSettlement,
            "ShopOrder",
            Guid.NewGuid(),
            idempotencyKey,
            "Phân bổ doanh thu hợp lệ",
            lines);

        // Assert
        _mockLedgerRepo.Verify(r => r.AddTransactionAsync(
            It.Is<LedgerTransaction>(tx => tx.IdempotencyKey == idempotencyKey && tx.Entries.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ShopBalanceCache_ApplyOrderSettlement_ShouldCorrectlyUpdateBalances()
    {
        // Arrange
        var shopId = Guid.NewGuid();
        var cache = new ShopBalanceCache(shopId);

        // Act
        cache.ApplyOrderSettlement(grossAmount: 100_000, commissionAmount: 10_000, netAmount: 90_000);

        // Assert
        cache.PendingBalance.Should().Be(90_000);
        cache.LifetimeGrossRevenue.Should().Be(100_000);
        cache.LifetimeCommissionPaid.Should().Be(10_000);
        cache.AvailableBalance.Should().Be(0);
        cache.Version.Should().Be(2);
    }

    [Fact]
    public void ShopBalanceCache_ReleasePendingToAvailable_ShouldTransferFunds()
    {
        // Arrange
        var shopId = Guid.NewGuid();
        var cache = new ShopBalanceCache(shopId);
        cache.ApplyOrderSettlement(100_000, 10_000, 90_000);

        // Act
        cache.ReleasePendingToAvailable(90_000);

        // Assert
        cache.PendingBalance.Should().Be(0);
        cache.AvailableBalance.Should().Be(90_000);
    }

    [Fact]
    public void ShopBalanceCache_HoldPayout_WhenInsufficientAvailableBalance_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var shopId = Guid.NewGuid();
        var cache = new ShopBalanceCache(shopId);

        // Act & Assert
        var act = () => cache.HoldPayout(50_000);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*không đủ để rút*");
    }
}

