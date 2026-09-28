namespace FieldFlow.Application.Errors;

/// <summary>
/// Exceção de borda da aplicação: carrega código (SCREAMING_SNAKE_CASE) e status HTTP.
/// O domínio NUNCA lança esta exceção — ele devolve <c>Result</c>. Os handlers traduzem
/// uma falha de domínio em <see cref="AppException"/> via <c>ResultExtensions.GetValueOrThrow()</c>,
/// e o <c>ExceptionHandlingMiddleware</c> converte em Problem Details.
/// </summary>
public class AppException : Exception
{
    public AppException(string code, string message, int statusCode,
                        object? details = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        StatusCode = statusCode;
        Details = details;
    }

    public string Code { get; }
    public int StatusCode { get; }
    public object? Details { get; }
}
