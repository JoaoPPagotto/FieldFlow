using FieldFlow.Api.Services;
using FieldFlow.Application.Abstractions;

namespace FieldFlow.Api.Extensions;

public static class PresentationExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        // Autenticação JWT própria entra na Fase 2. Até lá, registra o esquema vazio
        // para que UseAuthentication/UseAuthorization funcionem.
        services.AddAuthentication();
        services.AddAuthorization();

        return services;
    }
}
