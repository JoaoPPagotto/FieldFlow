namespace FieldFlow.Domain.WorkOrders;

public enum WorkOrderEventType
{
    Created = 1,
    Opened = 2,
    Assigned = 3,
    Started = 4,
    Held = 5,
    Resumed = 6,
    Completed = 7,
    Cancelled = 8,
    TimeEntryAdded = 20,
    PartUsed = 21,
    ChecklistItemAdded = 22,
    ChecklistItemAnswered = 23,
    AttachmentAdded = 24
}
