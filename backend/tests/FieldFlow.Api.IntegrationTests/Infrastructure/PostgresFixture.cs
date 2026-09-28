using Testcontainers.PostgreSql;

namespace FieldFlow.Api.IntegrationTests.Infrastructure;

/// <summary>Sobe um PostgreSQL 16 real (Testcontainers) compartilhado pela coleção de testes. Requer Docker.</summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("fieldflow_tests")
        .WithUsername("fieldflow")
        .WithPassword("fieldflow")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
