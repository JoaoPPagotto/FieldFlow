using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FieldFlow.Api.Filters;

/// <summary>Declara um header obrigatório na action, exibido no Swagger (ex.: <c>Idempotency-Key</c>).</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class CustomHeaderAttribute : Attribute
{
    public CustomHeaderAttribute(string name, bool required = true)
    {
        Name = name;
        Required = required;
    }

    public string Name { get; }
    public bool Required { get; }
}

/// <summary>
/// Adiciona no Swagger: headers de <see cref="CustomHeaderAttribute"/> e, para actions de coleção
/// com <c>[EnableQuery]</c>, os parâmetros <c>$filter</c>, <c>$orderby</c>, <c>$top</c>, <c>$skip</c>,
/// <c>$select</c>, <c>$expand</c> e <c>$count</c>.
/// </summary>
public sealed class ODataQueryOptionsOperationFilter : IOperationFilter
{
    private static readonly string[] StringOptions = ["$filter", "$orderby", "$select", "$expand"];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        foreach (var header in context.MethodInfo.GetCustomAttributes(typeof(CustomHeaderAttribute), true)
                     .Cast<CustomHeaderAttribute>())
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = header.Name,
                In = ParameterLocation.Header,
                Required = header.Required,
                Schema = new OpenApiSchema { Type = "string" }
            });
        }

        var hasEnableQuery = context.MethodInfo.GetCustomAttributes(typeof(EnableQueryAttribute), true).Length > 0;
        var path = context.ApiDescription.RelativePath ?? string.Empty;
        var isSingle = path.Contains("{key}", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("{id}", StringComparison.OrdinalIgnoreCase);
        if (!hasEnableQuery || isSingle) return;

        foreach (var name in StringOptions)
            operation.Parameters.Add(Query(name, "string"));
        operation.Parameters.Add(Query("$top", "integer"));
        operation.Parameters.Add(Query("$skip", "integer"));
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "$count",
            In = ParameterLocation.Query,
            Required = false,
            Schema = new OpenApiSchema { Type = "boolean", Default = new OpenApiBoolean(false) }
        });
    }

    private static OpenApiParameter Query(string name, string type) => new()
    {
        Name = name,
        In = ParameterLocation.Query,
        Required = false,
        Schema = new OpenApiSchema { Type = type }
    };
}
