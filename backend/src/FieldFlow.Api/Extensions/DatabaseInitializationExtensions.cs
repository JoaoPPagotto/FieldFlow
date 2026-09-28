using FieldFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FieldFlow.Api.Extensions;

public static class DatabaseInitializationExtensions
{
    /// <summary>
    /// Aplica as migrations pendentes. Nunca derruba o boot: se o banco estiver fora,
    /// loga warning e o <c>/health/ready</c> passa a reportar Unhealthy.
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
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database initialization failed. Continuing startup.");
        }
    }
}
