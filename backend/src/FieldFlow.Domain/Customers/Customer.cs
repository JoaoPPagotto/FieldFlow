using System.Net.Mail;
using FieldFlow.Domain.Assets;
using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.Customers;

/// <summary>Cliente atendido (empresa). Agregado raiz.</summary>
public sealed class Customer : AggregateRoot
{
    public const int NameMaxLength = 200;

    private readonly List<Asset> _assets = [];

    private Customer() { } // EF Core

    private Customer(Guid id, string name, Cnpj document, string email, string phone, Address address)
    {
        CustomerId = id;
        Name = name;
        Document = document;
        Email = email;
        Phone = phone;
        Address = address;
    }

    public Guid CustomerId { get; private set; }
    public string Name { get; private set; } = null!;
    public Cnpj Document { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public Address Address { get; private set; } = null!;

    /// <summary>Somente leitura: ativos são um agregado próprio, criados por <see cref="Asset.Create"/>.</summary>
    public IReadOnlyCollection<Asset> Assets => _assets.AsReadOnly();

    public static Result<Customer> Create(string? name, string? document, string? email, string? phone, Address address)
    {
        var validation = Validate(name, document, email, phone, out var cnpj, out var normalizedPhone);
        if (validation.IsFailure) return validation.Error;

        return new Customer(Guid.NewGuid(), name!.Trim(), cnpj!, email!.Trim().ToLowerInvariant(), normalizedPhone, address);
    }

    public Result Update(string? name, string? document, string? email, string? phone, Address address)
    {
        var validation = Validate(name, document, email, phone, out var cnpj, out var normalizedPhone);
        if (validation.IsFailure) return validation;

        Name = name!.Trim();
        Document = cnpj!;
        Email = email!.Trim().ToLowerInvariant();
        Phone = normalizedPhone;
        Address = address;
        return Result.Success();
    }

    private static Result Validate(string? name, string? document, string? email, string? phone,
                                   out Cnpj? cnpj, out string normalizedPhone)
    {
        cnpj = null;
        normalizedPhone = Guard.OnlyDigits(phone);

        var nameResult = Guard.Required(name, NameMaxLength, CustomerErrors.NameRequired, CustomerErrors.NameTooLong);
        if (nameResult.IsFailure) return nameResult;

        var cnpjResult = Cnpj.Create(document);
        if (cnpjResult.IsFailure) return cnpjResult.Error;
        cnpj = cnpjResult.Value;

        if (!IsValidEmail(email)) return CustomerErrors.InvalidEmail;
        if (normalizedPhone.Length is not (10 or 11)) return CustomerErrors.InvalidPhone;

        return Result.Success();
    }

    internal static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254) return false;
        return MailAddress.TryCreate(email.Trim(), out var parsed) && parsed.Address == email.Trim();
    }
}
