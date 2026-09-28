using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.UnitTests.Common;

public class ResultTests
{
    private static readonly Error SampleError = Error.Validation("SAMPLE", "Falhou");

    [Fact]
    public void Implicit_conversions_build_success_and_failure()
    {
        Result<int> success = 42;
        Result<int> failure = SampleError;

        success.IsSuccess.Should().BeTrue();
        success.Value.Should().Be(42);
        failure.IsFailure.Should().BeTrue();
        failure.Error.Should().Be(SampleError);
    }

    [Fact]
    public void Accessing_value_of_failure_throws()
    {
        Result<int> failure = SampleError;

        var act = () => failure.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FirstFailureOrSuccess_returns_first_failure()
    {
        var other = Error.Conflict("OTHER", "x");

        var result = Result.FirstFailureOrSuccess(Result.Success(), Result.Failure(SampleError), Result.Failure(other));

        result.Error.Should().Be(SampleError);
    }
}
