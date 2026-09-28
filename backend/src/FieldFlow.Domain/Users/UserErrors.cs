using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.Users;

public static class UserErrors
{
    public static readonly Error InvalidEmail = Error.Validation("USER_EMAIL_INVALID", "E-mail inválido.");
    public static readonly Error NameRequired = Error.Validation("USER_NAME_REQUIRED", "Nome do usuário é obrigatório.");
    public static readonly Error PasswordHashRequired = Error.Validation("USER_PASSWORD_HASH_REQUIRED", "Hash de senha é obrigatório.");
    public static readonly Error EmailAlreadyExists = Error.Conflict("USER_EMAIL_ALREADY_EXISTS", "Já existe um usuário com este e-mail.");
    public static readonly Error NotFound = Error.NotFound("USER_NOT_FOUND", "Usuário não encontrado.");
}
