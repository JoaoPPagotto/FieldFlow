using FieldFlow.Application.Errors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace FieldFlow.Application.Behaviors;

/// <summary>
/// Pipeline MediatR: roda antes de todo handler, agrega as falhas de todos os
/// <see cref="IValidator{T}"/> do request e lança <see cref="AppException"/> 400.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = new List<ValidationFailure>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            if (result.Errors.Count > 0)
                failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
        {
            var details = failures
                .Select(e => new ValidationErrorDetail(e.PropertyName, e.ErrorCode, e.ErrorMessage))
                .ToList();
            var code = failures.Select(e => e.ErrorCode).FirstOrDefault() ?? ErrorCodes.ValidationFailed;
            throw new AppException(code, "Validation failed", 400, details);
        }

        return await next();
    }
}

public sealed record ValidationErrorDetail(string PropertyName, string Code, string Message);
