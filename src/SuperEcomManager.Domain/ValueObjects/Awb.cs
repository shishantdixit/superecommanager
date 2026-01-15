namespace SuperEcomManager.Domain.ValueObjects;

/// <summary>
/// Value object representing an Air Waybill (AWB) / Tracking number.
/// </summary>
public sealed class Awb : IEquatable<Awb>
{
    public string Value { get; }
    public string CourierCode { get; }

    public Awb(string value, string courierCode)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("AWB cannot be empty", nameof(value));
        if (string.IsNullOrWhiteSpace(courierCode))
            throw new ArgumentException("Courier code cannot be empty", nameof(courierCode));

        Value = value.Trim().ToUpperInvariant();
        CourierCode = courierCode.Trim().ToUpperInvariant();
    }

    public bool Equals(Awb? other)
    {
        if (other is null) return false;
        return Value == other.Value && CourierCode == other.CourierCode;
    }

    public override bool Equals(object? obj) => Equals(obj as Awb);

    public override int GetHashCode() => HashCode.Combine(Value, CourierCode);

    public override string ToString() => $"{CourierCode}:{Value}";

    public static bool operator ==(Awb? a, Awb? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Awb? a, Awb? b) => !(a == b);
}
