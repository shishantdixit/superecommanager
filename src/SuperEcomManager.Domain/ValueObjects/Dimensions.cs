namespace SuperEcomManager.Domain.ValueObjects;

/// <summary>
/// Value object representing package dimensions (for shipping).
/// </summary>
public sealed class Dimensions : IEquatable<Dimensions>
{
    /// <summary>Length in centimeters</summary>
    public decimal LengthCm { get; private set; }

    /// <summary>Width in centimeters</summary>
    public decimal WidthCm { get; private set; }

    /// <summary>Height in centimeters</summary>
    public decimal HeightCm { get; private set; }

    /// <summary>Weight in kilograms</summary>
    public decimal WeightKg { get; private set; }

    // Required for EF Core
    private Dimensions() { }

    public Dimensions(decimal lengthCm, decimal widthCm, decimal heightCm, decimal weightKg)
    {
        if (lengthCm <= 0)
            throw new ArgumentException("Length must be positive", nameof(lengthCm));
        if (widthCm <= 0)
            throw new ArgumentException("Width must be positive", nameof(widthCm));
        if (heightCm <= 0)
            throw new ArgumentException("Height must be positive", nameof(heightCm));
        if (weightKg <= 0)
            throw new ArgumentException("Weight must be positive", nameof(weightKg));

        LengthCm = lengthCm;
        WidthCm = widthCm;
        HeightCm = heightCm;
        WeightKg = weightKg;
    }

    /// <summary>
    /// Calculate volumetric weight (for shipping rate calculation).
    /// Formula: (L x W x H) / 5000
    /// </summary>
    public decimal GetVolumetricWeightKg()
    {
        return (LengthCm * WidthCm * HeightCm) / 5000;
    }

    /// <summary>
    /// Get the chargeable weight (higher of actual or volumetric).
    /// </summary>
    public decimal GetChargeableWeightKg()
    {
        return Math.Max(WeightKg, GetVolumetricWeightKg());
    }

    public bool Equals(Dimensions? other)
    {
        if (other is null) return false;
        return LengthCm == other.LengthCm
            && WidthCm == other.WidthCm
            && HeightCm == other.HeightCm
            && WeightKg == other.WeightKg;
    }

    public override bool Equals(object? obj) => Equals(obj as Dimensions);

    public override int GetHashCode() => HashCode.Combine(LengthCm, WidthCm, HeightCm, WeightKg);

    public override string ToString() => $"{LengthCm}x{WidthCm}x{HeightCm}cm, {WeightKg}kg";

    public static bool operator ==(Dimensions? a, Dimensions? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Dimensions? a, Dimensions? b) => !(a == b);
}
