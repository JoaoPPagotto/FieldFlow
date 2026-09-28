using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using FieldFlow.Domain.Common;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace FieldFlow.Api.Extensions;

public static class ODataExtensions
{
    /// <summary>Prefixo das rotas OData (convenção REST do projeto: /api/v1).</summary>
    public const string RoutePrefix = "api/v1";

    public static IServiceCollection AddODataServices(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            })
            .AddOData(options =>
            {
                options.EnableAttributeRouting = false;
                options.UrlKeyDelimiter = Microsoft.OData.ODataUrlKeyDelimiter.Slash;

                options.EnableQueryFeatures()
                    .Select().Expand().Filter().OrderBy().Count().SkipToken().SetMaxTop(200);

                options.AddRouteComponents(RoutePrefix, GetEdmModel());
            });

        return services;
    }

    /// <summary>
    /// ⚠️ Toda entidade exposta via OData PRECISA ser registrada aqui — sem isso a rota não existe.
    /// Os EntitySets entram a partir da Fase 3 (cadastros).
    /// </summary>
    internal static IEdmModel GetEdmModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EnableLowerCamelCase();

        // Exemplo (Fase 3):
        // var customers = builder.EntitySet<Customer>("customers");
        // customers.EntityType.HasKey(e => e.CustomerId);
        // customers.EntityType.IgnoreAuditProperties();

        return builder.GetEdmModel();
    }

    /// <summary>Remove do EDM as propriedades de auditoria do <see cref="BaseEntity"/>, exceto CreatedAt e UpdatedAt.</summary>
    public static void IgnoreAuditProperties<TEntity>(this StructuralTypeConfiguration<TEntity> entity)
        where TEntity : class
        => entity.IgnoreBaseEntityProperties(nameof(BaseEntity.CreatedAt), nameof(BaseEntity.UpdatedAt));

    public static void IgnoreBaseEntityProperties<TEntity>(
        this StructuralTypeConfiguration<TEntity> entity, params string[] keep)
        where TEntity : class
    {
        var keepSet = new HashSet<string>(keep, StringComparer.OrdinalIgnoreCase);
        var ignore = typeof(StructuralTypeConfiguration<TEntity>).GetMethod(nameof(entity.Ignore))!;

        foreach (var property in typeof(BaseEntity).GetProperties())
        {
            if (keepSet.Contains(property.Name)) continue;

            // Ignore(e => e.<Property>) montado por reflexão (a API só aceita expressão tipada)
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var lambda = Expression.Lambda(Expression.Property(parameter, property.Name), parameter);
            ignore.MakeGenericMethod(property.PropertyType).Invoke(entity, [lambda]);
        }
    }
}
