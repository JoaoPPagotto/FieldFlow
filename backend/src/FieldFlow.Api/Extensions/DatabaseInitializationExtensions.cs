using FieldFlow.Infrastructure.Persistence;
using FieldFlow.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace FieldFlow.Api.Extensions;

public static class DatabaseInitializationExtensions
{
    /// <summary>
    /// Aplica as migrations pendentes e, se <c>Seed:DemoData</c> = true, popula dados de demonstração.
    /// Nunca derruba o boot: se o banco estiver fora, loga warning e o <c>/health/ready</c> reporta Unhealthy.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (app.Configuration.GetValue("Database:SkipInitialization", false))
            return;

        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitialization");

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (context.Database.GetMigrations().Any())
            {
                logger.LogInformation("Applying database migrations...");
                await context.Database.MigrateAsync();
            }
            else
            {
                // Ainda não há migration versionada (ver README → "Migrations"). Cria o schema direto do modelo.
                logger.LogWarning("No EF Core migrations found — creating schema with EnsureCreated. " +
                                  "Run scripts/add-migration to version the schema.");
                await context.Database.EnsureCreatedAsync();
            }

            if (app.Configuration.GetValue("Seed:DemoData", false))
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
                await seeder.SeedAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database initialization failed. Continuing startup.");
        }
    }
}
