using NetArchTest.Rules;

namespace FieldFlow.Api.IntegrationTests.Architecture;

/// <summary>
/// Garante as regras de dependência da Clean Architecture. Quebra o build se alguém,
/// por exemplo, referenciar EF Core no Domain.
/// </summary>
public class LayerDependencyTests
{
    private const string Domain = "FieldFlow.Domain";
    private const string Application = "FieldFlow.Application";
    private const string Infrastructure = "FieldFlow.Infrastructure";
    private const string Api = "FieldFlow.Api";

    [Fact]
    public void Domain_does_not_depend_on_other_layers()
    {
        var result = Types.InAssembly(FieldFlow.Domain.AssemblyReference.Assembly)
            .ShouldNot().HaveDependencyOnAny(Application, Infrastructure, Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Failing(result));
    }

    [Fact]
    public void Domain_does_not_depend_on_frameworks()
    {
        var result = Types.InAssembly(FieldFlow.Domain.AssemblyReference.Assembly)
            .ShouldNot().HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore", "MediatR", "FluentValidation", "AutoMapper",
                "Microsoft.AspNetCore", "Microsoft.Extensions", "Npgsql")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Failing(result));
    }

    [Fact]
    public void Application_does_not_depend_on_Infrastructure_or_Api()
    {
        var result = Types.InAssembly(FieldFlow.Application.AssemblyReference.Assembly)
            .ShouldNot().HaveDependencyOnAny(Infrastructure, Api, "Microsoft.EntityFrameworkCore", "Npgsql")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Failing(result));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_Api()
    {
        var result = Types.InAssembly(FieldFlow.Infrastructure.AssemblyReference.Assembly)
            .ShouldNot().HaveDependencyOn(Api)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Failing(result));
    }

    private static string Failing(TestResult result)
        => "tipos violando a regra: " + string.Join(", ", result.FailingTypeNames ?? []);
}
