using FieldFlow.Application.Behaviors;
using FieldFlow.Application.Errors;
using FluentValidation;
using MediatR;

namespace FieldFlow.Application.UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    public sealed record SampleRequest(string Name) : IRequest<string>;

    private sealed class SampleValidator : AbstractValidator<SampleRequest>
    {
        public SampleValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ValidationMessages.NotEmpty).WithErrorCode("SAMPLE_NAME_REQUIRED")
                .WithName("Nome");
        }
    }

    [Fact]
    public async Task Calls_next_when_there_are_no_validators()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>([]);

        var result = await behavior.Handle(new SampleRequest(""), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Calls_next_when_request_is_valid()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>([new SampleValidator()]);

        var result = await behavior.Handle(new SampleRequest("Filtro"), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Throws_AppException_400_with_first_error_code_when_invalid()
    {
        var behavior = new ValidationBehavior<SampleRequest, string>([new SampleValidator()]);
        var nextCalled = false;

        var act = () => behavior.Handle(new SampleRequest(""), () =>
        {
            nextCalled = true;
            return Task.FromResult("ok");
        }, CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<AppException>()).Which;
        ex.StatusCode.Should().Be(400);
        ex.Code.Should().Be("SAMPLE_NAME_REQUIRED");
        ex.Details.Should().BeAssignableTo<IEnumerable<ValidationErrorDetail>>()
            .Which.Should().ContainSingle(d => d.Message == "Nome é obrigatório");
        nextCalled.Should().BeFalse();
    }
}
