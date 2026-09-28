using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.Customers;

/// <summary>Endereço (value object, persistido como owned type na tabela do cliente).</summary>
public sealed record Address
{
    private Address(string street, string number, string city, string state, string zipCode)
    {
        Street = street;
        Number = number;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    public string Street { get; }
    public string Number { get; }
    public string City { get; }
    /// <summary>UF, 2 letras maiúsculas.</summary>
    public string State { get; }
    /// <summary>CEP, 8 dígitos.</summary>
    public string ZipCode { get; }

    public static Result<Address> Create(string? street, string? number, string? city, string? state, string? zipCode)
    {
        var zip = Guard.OnlyDigits(zipCode);
        var uf = (state ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(street) || street.Trim().Length > 200) return CustomerErrors.InvalidAddress("street");
        if (string.IsNullOrWhiteSpace(number) || number.Trim().Length > 20) return CustomerErrors.InvalidAddress("number");
        if (string.IsNullOrWhiteSpace(city) || city.Trim().Length > 100) return CustomerErrors.InvalidAddress("city");
        if (uf.Length != 2 || !uf.All(char.IsAsciiLetterUpper)) return CustomerErrors.InvalidAddress("state");
        if (zip.Length != 8) return CustomerErrors.InvalidAddress("zipCode");

        return new Address(street.Trim(), number.Trim(), city.Trim(), uf, zip);
    }
}
