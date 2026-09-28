using FieldFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FieldFlow.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Sobe a API em memória apontando para o Postgres do Testcontainers.
/// Use <see cref="WithIsolatedDatabase"/> para ter um banco exclusivo por teste/classe.
/// </summary>
public sealed class FieldFlowApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly bool _seedDemoData;

    public FieldFlowApiFactory(string connectionString, bool seedDemoData = false)
    {
        _connectionString = connectionString;
        _seedDemoData = seedDemoData;
    }

    /// <summary>Mesmo servidor, banco novo (nome aleatório) — criado no boot da API.</summary>
    public static FieldFlowApiFactory WithIsolatedDatabase(string serverConnectionString, bool seedDemoData = false)
    {
        var builder = new NpgsqlConnectionStringBuilder(serverConnectionString)
        {
            Database = $"fieldflow_{Guid.NewGuid():N}"
        };
        return new FieldFlowApiFactory(builder.ConnectionString, seedDemoData);
    }

    /// <summary>Executa código com um <see cref="ApplicationDbContext"/> de um escopo novo.</summary>
    public async Task<T> WithDbContextAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(context);
    }

    public async Task WithScopeAsync(Func<IServiceProvider, Task> action)
    {
        await using var scope = Services.CreateAsyncScope();
        await action(scope.ServiceProvider);
    }

    public async Task<T> QueryScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Seed:DemoData", _seedDemoData ? "true" : "false");
    }
}
