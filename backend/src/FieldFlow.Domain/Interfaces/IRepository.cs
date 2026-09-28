using System.Linq.Expressions;

namespace FieldFlow.Domain.Interfaces;

/// <summary>Repositório genérico. Repositórios específicos por agregado estendem esta interface.</summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T?> FindFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    void Update(T entity);

    /// <summary>Marca para exclusão. A Infrastructure converte em <b>soft delete</b> — nunca há hard delete.</summary>
    void Delete(T entity);

    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>Consulta componível (usada pelo OData no GetAll).</summary>
    IQueryable<T> Query();

    void ClearTracking();
}
