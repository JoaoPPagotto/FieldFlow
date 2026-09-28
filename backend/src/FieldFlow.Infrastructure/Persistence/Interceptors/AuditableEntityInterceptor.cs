using FieldFlow.Application.Abstractions;
using FieldFlow.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FieldFlow.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Preenche as colunas de auditoria e converte <b>todo</b> DELETE de <see cref="BaseEntity"/>
/// em soft delete (UPDATE is_deleted = true). Não existe hard delete pela aplicação.
/// </summary>
internal sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public AuditableEntityInterceptor(ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _clock = clock;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null) return;

        var user = _currentUser.AuditName;
        var now = _clock.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // Respeita valores já definidos (ex.: seed/importação com data histórica)
                    entry.Entity.SetCreated(
                        entry.Entity.CreatedBy ?? user,
                        entry.Entity.CreatedAt == default ? now : entry.Entity.CreatedAt);
                    break;

                case EntityState.Modified:
                    entry.Entity.SetUpdated(user, now);
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.MarkDeleted(user, now);
                    entry.Entity.SetUpdated(user, now);
                    break;
            }
        }
    }
}
