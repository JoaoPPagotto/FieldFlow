using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FieldFlow.Api.IntegrationTests.Infrastructure;

/// <summary>Sobe a API em memória apontando para o Postgres do Testcontainers.</summary>
public sealed class FieldFlowApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly bool _seedDemoData;

    public FieldFlowApiFactory(string connectionString, bool seedDemoData = false)
    {
        _connectionString = connectionString;
        _seedDemoData = seedDemoData;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Seed:DemoData", _seedDemoData ? "true" : "false");
    }
}
