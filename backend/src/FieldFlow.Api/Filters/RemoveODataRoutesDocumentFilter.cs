using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FieldFlow.Api.Filters;

/// <summary>Remove do OpenAPI as rotas duplicadas geradas pelo OData (<c>({key})</c>, <c>/$count</c>, <c>$metadata</c>).</summary>
public sealed class RemoveODataRoutesDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var toRemove = swaggerDoc.Paths.Keys
            .Where(p => p.Contains("({", StringComparison.Ordinal)
                     || p.Contains("{{", StringComparison.Ordinal)
                     || p.EndsWith("/$count", StringComparison.Ordinal)
                     || p.Contains("$metadata", StringComparison.Ordinal)
                     || p.EndsWith("/api/v1", StringComparison.Ordinal))
            .ToList();

        foreach (var path in toRemove)
            swaggerDoc.Paths.Remove(path);
    }
}
