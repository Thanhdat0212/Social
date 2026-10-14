using System.Globalization;

namespace Domain.Common;

/// <summary>
/// Value Object đại diện cho số tiền (VND).
/// Toàn bộ tiền tệ trong hệ thống dùng kiểu 'long' (VND, không có số lẻ), cột Database là 'bigint'.
/// </summary>
public readonly struct Money : IEquatable<Money>, IComparable<Money>
{
    public long Amount { get; }
    public const string Currency = "VND";

    public Money(long amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền không thể là số âm.");
        }
        Amount = amount;
    }

    public static Money Zero => new(0);

    public static Money FromVnd(long amount) => new(amount);

    public static Money operator +(Money a, Money b) => new(a.Amount + b.Amount);

    public static Money operator -(Money a, Money b)
    {
        if (a.Amount < b.Amount)
        {
            throw new InvalidOperationException("Số dư không đủ để thực hiện phép trừ tiền tệ.");
        }
        return new(a.Amount - b.Amount);
    }

    public static bool operator >(Money a, Money b) => a.Amount > b.Amount;
    public static bool operator <(Money a, Money b) => a.Amount < b.Amount;
    public static bool operator >=(Money a, Money b) => a.Amount >= b.Amount;
    public static bool operator <=(Money a, Money b) => a.Amount <= b.Amount;
    public static bool operator ==(Money a, Money b) => a.Amount == b.Amount;
    public static bool operator !=(Money a, Money b) => a.Amount != b.Amount;

    public bool Equals(Money other) => Amount == other.Amount;

    public override bool Equals(object? obj) => obj is Money other && Equals(other);

    public override int GetHashCode() => Amount.GetHashCode();

    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

    public override string ToString()
    {
        return $"{Amount.ToString("N0", new CultureInfo("vi-VN"))} ₫";
    }

    public static implicit operator long(Money money) => money.Amount;
    public static implicit operator Money(long amount) => new(amount);
}
