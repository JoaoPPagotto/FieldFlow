namespace FieldFlow.Domain.WorkOrders;

/// <summary>
/// Item imutável da timeline da OS. Gerado exclusivamente pelos métodos do <see cref="WorkOrder"/> —
/// auditoria é efeito do domínio, não do controller. Não herda BaseEntity: nunca é alterado nem excluído.
/// </summary>
public sealed class WorkOrderEvent
{
    private WorkOrderEvent() { } // EF Core

    internal WorkOrderEvent(Guid workOrderId, WorkOrderEventType type, string actor, DateTime occurredAtUtc,
                            WorkOrderStatus? fromStatus, WorkOrderStatus? toStatus, string? payload)
    {
        WorkOrderEventId = Guid.NewGuid();
        WorkOrderId = workOrderId;
        Type = type;
        Actor = actor;
        OccurredAt = occurredAtUtc;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Payload = payload;
    }

    public Guid WorkOrderEventId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public WorkOrderEventType Type { get; private set; }
    public string Actor { get; private set; } = null!;
    public DateTime OccurredAt { get; private set; }
    public WorkOrderStatus? FromStatus { get; private set; }
    public WorkOrderStatus? ToStatus { get; private set; }

    /// <summary>Detalhes do evento em JSON (motivo, técnico, peça...).</summary>
    public string? Payload { get; private set; }
}
