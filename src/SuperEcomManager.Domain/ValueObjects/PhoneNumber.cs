using System.Text.RegularExpressions;

namespace SuperEcomManager.Domain.ValueObjects;

/// <summary>
/// Value object representing a phone number.
/// </summary>
public sealed partial class PhoneNumber : IEquatable<PhoneNumber>
{
    public string CountryCode { get; }
    public string Number { get; }

    public PhoneNumber(string number, string countryCode = "+91")
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Phone number cannot be empty", nameof(number));

        // Clean the number - remove spaces, dashes, etc.
        var cleanNumber = CleanPhoneRegex().Replace(number, "");

        // If number starts with country code, extract it
        if (cleanNumber.StartsWith('+'))
        {
            // Handle Indian numbers: +91XXXXXXXXXX
            if (cleanNumber.StartsWith("+91") && cleanNumber.Length == 13)
            {
                CountryCode = "+91";
                Number = cleanNumber[3..];
            }
            else
            {
                // Generic handling for other country codes
                var match = PhoneWithCountryCodeRegex().Match(cleanNumber);
                if (match.Success)
                {
                    CountryCode = match.Groups[1].Value;
                    Number = match.Groups[2].Value;
                }
                else
                {
                    throw new ArgumentException("Invalid phone number format", nameof(number));
                }
            }
        }
        else
        {
            // Number without country code
            if (cleanNumber.Length == 10 && cleanNumber.All(char.IsDigit))
            {
                CountryCode = countryCode;
                Number = cleanNumber;
            }
            else
            {
                throw new ArgumentException("Phone number must be 10 digits", nameof(number));
            }
        }
    }

    /// <summary>
    /// Returns the full phone number with country code.
    /// </summary>
    public string ToFullNumber() => $"{CountryCode}{Number}";

    /// <summary>
    /// Returns masked phone number for display (e.g., 98****3210).
    /// </summary>
    public string ToMasked()
    {
        if (Number.Length < 4)
            return new string('*', Number.Length);

        return $"{Number[..2]}****{Number[^4..]}";
    }

    public bool Equals(PhoneNumber? other)
    {
        if (other is null) return false;
        return CountryCode == other.CountryCode && Number == other.Number;
    }

    public override bool Equals(object? obj) => Equals(obj as PhoneNumber);

    public override int GetHashCode() => HashCode.Combine(CountryCode, Number);

    public override string ToString() => ToFullNumber();

    public static bool operator ==(PhoneNumber? a, PhoneNumber? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(PhoneNumber? a, PhoneNumber? b) => !(a == b);

    [GeneratedRegex(@"[\s\-\(\)]")]
    private static partial Regex CleanPhoneRegex();

    [GeneratedRegex(@"^(\+\d{1,3})(\d+)$")]
    private static partial Regex PhoneWithCountryCodeRegex();
}
