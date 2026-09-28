using FieldFlow.Domain.Common;
using FieldFlow.Domain.Customers;

namespace FieldFlow.Domain.Users;

/// <summary>
/// Usuário de login. O hash é gerado fora do domínio (Infrastructure, Fase 2);
/// o domínio só garante que ele existe. Refresh tokens entram na Fase 2.
/// </summary>
public sealed class User : AggregateRoot
{
    private User() { } // EF Core

    public Guid UserId { get; private set; }
    public string Email { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }

    public static Result<User> Create(string? email, string? name, string? passwordHash, UserRole role)
    {
        if (!Customer.IsValidEmail(email)) return UserErrors.InvalidEmail;
        if (string.IsNullOrWhiteSpace(name)) return UserErrors.NameRequired;
        if (string.IsNullOrWhiteSpace(passwordHash)) return UserErrors.PasswordHashRequired;

        return new User
        {
            UserId = Guid.NewGuid(),
            Email = email!.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true
        };
    }

    public Result ChangePasswordHash(string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) return UserErrors.PasswordHashRequired;
        PasswordHash = passwordHash;
        return Result.Success();
    }

    public void ChangeRole(UserRole role) => Role = role;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
