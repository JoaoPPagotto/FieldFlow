namespace FieldFlow.Domain.Common;

/// <summary>
/// Resultado de uma operação de domínio. Regra de negócio violada NÃO lança exceção:
/// devolve <see cref="Failure(Error)"/> com um <see cref="Error"/> específico.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Um resultado de sucesso não pode conter erro.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Um resultado de falha precisa de um erro.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);

    /// <summary>Retorna a primeira falha, ou sucesso se todas passaram.</summary>
    public static Result FirstFailureOrSuccess(params Result[] results)
        => results.FirstOrDefault(r => r.IsFailure) ?? Success();

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;

    /// <summary>Valor do sucesso. Acessar em uma falha é bug de programação → exceção.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Não é possível acessar o valor de um resultado com falha ({Error.Code}).");

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
