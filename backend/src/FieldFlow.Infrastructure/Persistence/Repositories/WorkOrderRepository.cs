using FieldFlow.Domain.WorkOrders;
using Microsoft.EntityFrameworkCore;

namespace FieldFlow.Infrastructure.Persistence.Repositories;

internal sealed class WorkOrderRepository : Repository<WorkOrder>, IWorkOrderRepository
{
    private const string NextNumberSql = "SELECT nextval('fieldflow.work_order_number_seq') AS \"Value\"";

    public WorkOrderRepository(ApplicationDbContext context) : base(context) { }

    public Task<WorkOrder?> GetWithDetailsAsync(Guid workOrderId, CancellationToken cancellationToken = default)
        => DbSet
            .Include(w => w.TimeEntries)
            .Include(w => w.UsedParts)
            .Include(w => w.ChecklistItems)
            .Include(w => w.Attachments)
            .Include(w => w.Events)
            .AsSplitQuery()
            .FirstOrDefaultAsync(w => w.WorkOrderId == workOrderId, cancellationToken);

    public async Task<WorkOrderNumber> NextNumberAsync(int year, CancellationToken cancellationToken = default)
    {
        var sequence = await Context.Database
            .SqlQueryRaw<long>(NextNumberSql)
            .SingleAsync(cancellationToken);

        return WorkOrderNumber.From(year, sequence);
    }
}
