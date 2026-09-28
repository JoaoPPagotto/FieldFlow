using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.WorkOrders;

/// <summary>Apontamento de horas de um técnico na OS.</summary>
public sealed class TimeEntry : BaseEntity
{
    private TimeEntry() { } // EF Core

    internal TimeEntry(Guid workOrderId, Guid technicianId, DateTime startedAt, DateTime endedAt, string? notes)
    {
        TimeEntryId = Guid.NewGuid();
        WorkOrderId = workOrderId;
        TechnicianId = technicianId;
        StartedAt = startedAt;
        EndedAt = endedAt;
        Notes = notes;
    }

    public Guid TimeEntryId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public Guid TechnicianId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime EndedAt { get; private set; }
    public string? Notes { get; private set; }

    public TimeSpan Duration => EndedAt - StartedAt;

    internal bool Overlaps(DateTime start, DateTime end) => start < EndedAt && StartedAt < end;
}
