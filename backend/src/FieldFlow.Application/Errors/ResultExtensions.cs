using FieldFlow.Domain.Common;

namespace FieldFlow.Application.Errors;

/// <summary>
/// Ponte entre o domínio (<see cref="Result"/>) e a borda (<see cref="AppException"/>).
/// Uso nos handlers: <c>workOrder.Start(actor, now).ThrowIfFailure();</c>
/// </summary>
public static class ResultExtensions
{
    public static void ThrowIfFailure(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsFailure) throw result.Error.ToAppException();
    }

    public static T GetValueOrThrow<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsFailure) throw result.Error.ToAppException();
        return result.Value;
    }

    public static AppException ToAppException(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new AppException(error.Code, error.Message, error.Type.ToStatusCode());
    }

    public static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => 400,
        ErrorType.Forbidden => 403,
        ErrorType.NotFound => 404,
        ErrorType.Conflict => 409,
        ErrorType.BusinessRule => 422,
        _ => 400
    };
}
