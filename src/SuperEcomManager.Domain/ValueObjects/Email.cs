using System.Text.RegularExpressions;

namespace SuperEcomManager.Domain.ValueObjects;

/// <summary>
/// Value object representing an email address.
/// </summary>
public sealed partial class Email : IEquatable<Email>
{
    public string Value { get; private set; }

    // Required for EF Core
    private Email()
    {
        Value = string.Empty;
    }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));

        value = value.Trim().ToLowerInvariant();

        if (!EmailRegex().IsMatch(value))
            throw new ArgumentException("Invalid email format", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Returns masked email for display (e.g., jo**@***.com).
    /// </summary>
    public string ToMasked()
    {
        var parts = Value.Split('@');
        if (parts.Length != 2)
            return "***@***.***";

        var localPart = parts[0];
        var domainPart = parts[1];

        var maskedLocal = localPart.Length <= 2
            ? new string('*', localPart.Length)
            : $"{localPart[..2]}{new string('*', Math.Min(localPart.Length - 2, 4))}";

        var domainParts = domainPart.Split('.');
        var maskedDomain = string.Join(".", domainParts.Select(p =>
            p.Length <= 2 ? new string('*', p.Length) : new string('*', 3)));

        return $"{maskedLocal}@{maskedDomain}";
    }

    public bool Equals(Email? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as Email);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;

    public static bool operator ==(Email? a, Email? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Email? a, Email? b) => !(a == b);

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex EmailRegex();
}
