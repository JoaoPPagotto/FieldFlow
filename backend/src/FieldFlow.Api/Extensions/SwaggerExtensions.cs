using FieldFlow.Api.Filters;
using Microsoft.OpenApi.Models;

namespace FieldFlow.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace('+', '.'));

            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FieldFlow API",
                Version = "v1",
                Description = "Field service management — Clean Architecture, CQRS, OData.",
                License = new OpenApiLicense { Name = "MIT" }
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT no header Authorization. Ex.: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });

            options.OperationFilter<ODataQueryOptionsOperationFilter>();
            options.DocumentFilter<RemoveODataRoutesDocumentFilter>();

            var xml = Path.Combine(AppContext.BaseDirectory, "FieldFlow.Api.xml");
            if (File.Exists(xml)) options.IncludeXmlComments(xml);
        });

        return services;
    }
}
