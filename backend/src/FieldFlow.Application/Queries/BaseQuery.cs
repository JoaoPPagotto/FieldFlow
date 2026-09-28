using MediatR;

namespace FieldFlow.Application.Queries;

/// <summary>
/// Base de toda query. Regra: o GetAll de coleção devolve <c>IQueryable&lt;TEntity&gt;</c>
/// para o <c>[EnableQuery]</c> do OData compor a expressão no banco; o GetById devolve o DTO.
/// </summary>
public abstract class BaseQuery<TResponse> : IRequest<TResponse>
{
    public string? UserId { get; set; }
}
