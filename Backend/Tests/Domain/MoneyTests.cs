using Domain.Common;
using FluentAssertions;

namespace Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithNegativeAmount_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => new Money(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Addition_ShouldSumAmountsCorrectly()
    {
        var a = Money.FromVnd(100_000);
        var b = Money.FromVnd(50_000);

        var result = a + b;

        result.Amount.Should().Be(150_000);
    }

    [Fact]
    public void Subtraction_WhenBalanceSufficient_ShouldReturnDifference()
    {
        var a = Money.FromVnd(100_000);
        var b = Money.FromVnd(40_000);

        var result = a - b;

        result.Amount.Should().Be(60_000);
    }

    [Fact]
    public void Subtraction_WhenBalanceInsufficient_ShouldThrowInvalidOperationException()
    {
        var a = Money.FromVnd(30_000);
        var b = Money.FromVnd(50_000);

        var act = () => a - b;

        act.Should().Throw<InvalidOperationException>();
    }
}

