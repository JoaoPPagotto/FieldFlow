namespace FieldFlow.Domain.Common;

/// <summary>Marcador de evento de domínio (publicado após o SaveChanges a partir das fases seguintes).</summary>
public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}
