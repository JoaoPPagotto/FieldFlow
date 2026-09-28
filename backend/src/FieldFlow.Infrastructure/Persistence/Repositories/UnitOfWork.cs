using FieldFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace FieldFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Unit of Work sobre o <see cref="ApplicationDbContext"/> (scoped). O ciclo de vida do contexto
/// pertence ao container de DI — por isso esta classe não o descarta.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork, IAsyncDisposable
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _transaction ??= await _context.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(cancellationToken);
        await DisposeTransactionAsync();
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(cancellationToken);
        await DisposeTransactionAsync();
    }

    public ValueTask DisposeAsync() => new(DisposeTransactionAsync());

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is null) return;
        await _transaction.DisposeAsync();
        _transaction = null;
    }
}
