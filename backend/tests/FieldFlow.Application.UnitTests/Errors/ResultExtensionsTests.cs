using FieldFlow.Application.Errors;
using FieldFlow.Domain.Common;

namespace FieldFlow.Application.UnitTests.Errors;

public class ResultExtensionsTests
{
    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Forbidden, 403)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.BusinessRule, 422)]
    public void Maps_error_type_to_http_status(ErrorType type, int expected)
    {
        type.ToStatusCode().Should().Be(expected);
    }

    [Fact]
    public void ThrowIfFailure_throws_AppException_with_domain_code()
    {
        var result = Result.Failure(Error.Conflict("WORK_ORDER_INVALID_TRANSITION", "Transição inválida: Draft → Completed."));

        var act = () => result.ThrowIfFailure();

        var ex = act.Should().Throw<AppException>().Which;
        ex.Code.Should().Be("WORK_ORDER_INVALID_TRANSITION");
        ex.StatusCode.Should().Be(409);
        ex.Message.Should().Be("Transição inválida: Draft → Completed.");
    }

    [Fact]
    public void GetValueOrThrow_returns_value_on_success()
    {
        Result<int> result = 7;

        result.GetValueOrThrow().Should().Be(7);
    }

    [Fact]
    public void ThrowIfFailure_does_nothing_on_success()
    {
        var act = () => Result.Success().ThrowIfFailure();

        act.Should().NotThrow();
    }
}
