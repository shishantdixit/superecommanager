namespace SuperEcomManager.Domain.ValueObjects;

/// <summary>
/// Value object representing a physical address.
/// </summary>
public sealed class Address : IEquatable<Address>
{
    public string Name { get; private set; }
    public string? Phone { get; private set; }
    public string Line1 { get; private set; }
    public string? Line2 { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }

    // Required for EF Core
    private Address()
    {
        Name = string.Empty;
        Line1 = string.Empty;
        City = string.Empty;
        State = string.Empty;
        PostalCode = string.Empty;
        Country = "India";
    }

    public Address(
        string name,
        string? phone,
        string line1,
        string? line2,
        string city,
        string state,
        string postalCode,
        string country = "India")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(line1))
            throw new ArgumentException("Address line 1 cannot be empty", nameof(line1));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty", nameof(city));
        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State cannot be empty", nameof(state));
        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code cannot be empty", nameof(postalCode));

        Name = name;
        Phone = phone;
        Line1 = line1;
        Line2 = line2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
    }

    public string GetFullAddress()
    {
        var parts = new List<string> { Line1 };

        if (!string.IsNullOrWhiteSpace(Line2))
            parts.Add(Line2);

        parts.Add(City);
        parts.Add($"{State} - {PostalCode}");
        parts.Add(Country);

        return string.Join(", ", parts);
    }

    public bool Equals(Address? other)
    {
        if (other is null) return false;

        return Name == other.Name
            && Phone == other.Phone
            && Line1 == other.Line1
            && Line2 == other.Line2
            && City == other.City
            && State == other.State
            && PostalCode == other.PostalCode
            && Country == other.Country;
    }

    public override bool Equals(object? obj) => Equals(obj as Address);

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Phone, Line1, Line2, City, State, PostalCode, Country);
    }

    public override string ToString() => GetFullAddress();

    public static bool operator ==(Address? a, Address? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Address? a, Address? b) => !(a == b);
}
