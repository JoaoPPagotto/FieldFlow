namespace FieldFlow.Domain.Common;

/// <summary>Validações simples reutilizadas pelos factory methods. Retornam <see cref="Result"/>, nunca lançam.</summary>
internal static class Guard
{
    public static Result NotEmpty(string? value, Error error)
        => string.IsNullOrWhiteSpace(value) ? Result.Failure(error) : Result.Success();

    public static Result MaxLength(string? value, int max, Error error)
        => value is not null && value.Trim().Length > max ? Result.Failure(error) : Result.Success();

    public static Result Required(string? value, int max, Error empty, Error tooLong)
        => Result.FirstFailureOrSuccess(NotEmpty(value, empty), MaxLength(value, max, tooLong));

    public static string OnlyDigits(string? value)
        => new((value ?? string.Empty).Where(char.IsDigit).ToArray());
}
