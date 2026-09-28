using System.Net;
using FieldFlow.Api.IntegrationTests.Infrastructure;

namespace FieldFlow.Api.IntegrationTests.Health;

[Collection(PostgresCollection.Name)]
public class HealthEndpointsTests
{
    private readonly PostgresFixture _postgres;

    public HealthEndpointsTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task Live_returns_200()
    {
        await using var factory = new FieldFlowApiFactory(_postgres.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_returns_200_when_database_is_reachable()
    {
        await using var factory = new FieldFlowApiFactory(_postgres.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_returns_503_when_database_is_unreachable()
    {
        await using var factory = new FieldFlowApiFactory(
            "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1");
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
