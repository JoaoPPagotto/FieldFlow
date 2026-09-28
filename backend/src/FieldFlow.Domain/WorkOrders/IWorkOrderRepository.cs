using FieldFlow.Domain.Interfaces;

namespace FieldFlow.Domain.WorkOrders;

public interface IWorkOrderRepository : IRepository<WorkOrder>
{
    /// <summary>Carrega o agregado completo (apontamentos, peças, checklist, anexos e timeline).</summary>
    Task<WorkOrder?> GetWithDetailsAsync(Guid workOrderId, CancellationToken cancellationToken = default);

    /// <summary>Próximo número legível (sequence do banco — seguro sob concorrência).</summary>
    Task<WorkOrderNumber> NextNumberAsync(int year, CancellationToken cancellationToken = default);
}
