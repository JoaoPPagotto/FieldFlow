using FieldFlow.Api.Extensions;
using FieldFlow.Api.Middleware;
using FieldFlow.Application;
using FieldFlow.Infrastructure;
using Serilog;

// Logger de bootstrap: captura erros antes do host estar configurado.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger(); // não-recarregável: evita "logger already frozen" com vários WebApplicationFactory nos testes

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 1) Serilog — console + arquivo JSON estruturado (configurado em appsettings "Serilog")
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "FieldFlow.Api"));

    // 2) Camadas
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddPresentation();

    // 3) Controllers + OData, Swagger, health checks
    builder.Services.AddODataServices();
    builder.Services.AddSwaggerDocumentation();
    builder.Services.AddFieldFlowHealthChecks();

    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(p => p
            .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod()));

    var app = builder.Build();

    // 4) Banco: migrations (+ seed demo). Falha aqui não derruba o boot — /health/ready reporta.
    await app.InitializeDatabaseAsync();

    // 5) Pipeline
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "FieldFlow API v1");
            c.DocumentTitle = "FieldFlow API";
        });
    }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapFieldFlowHealthChecks();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "FieldFlow.Api terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Exposto para o <c>WebApplicationFactory</c> dos testes de integração.</summary>
#pragma warning disable CA1050 // Program de top-level statements fica no namespace global
public partial class Program;
#pragma warning restore CA1050
