using FieldFlow.Application.Abstractions;
using FieldFlow.Domain.Interfaces;
using FieldFlow.Domain.Technicians;
using FieldFlow.Domain.WorkOrders;
using FieldFlow.Infrastructure.Calendar;
using FieldFlow.Infrastructure.Persistence;
using FieldFlow.Infrastructure.Persistence.Interceptors;
using FieldFlow.Infrastructure.Persistence.Repositories;
using FieldFlow.Infrastructure.Persistence.Seed;
using FieldFlow.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FieldFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<AuditableEntityInterceptor>();

        // Connection string resolvida de forma tardia (permite override nos testes de integração)
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada.");

            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", ApplicationDbContext.Schema))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>())
                // Filhos sem soft delete (timeline, skills) apontando para pais com query filter: esperado.
                .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        });

        // Repository + Unit of Work
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
        services.AddScoped<ITechnicianRepository, TechnicianRepository>();

        // Calendário comercial / SLA
        services.AddMemoryCache();
        services.Configure<BusinessCalendarOptions>(configuration.GetSection(BusinessCalendarOptions.SectionName));
        services.AddSingleton<IBusinessCalendarProvider, BusinessCalendarProvider>();

        // Seed de dados demo
        services.AddScoped<DemoDataSeeder>();

        return services;
    }
}
