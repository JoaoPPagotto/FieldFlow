namespace FieldFlow.Domain.WorkOrders;

/// <summary>
/// <code>
/// Draft ──► Open ──► Assigned ──► InProgress ──► Completed
///            │          │            │   ▲
///            │          │            ▼   │
///            │          │          OnHold ┘
///            ▼          ▼            │
///        Cancelled ◄────┴────────────┘
/// </code>
/// </summary>
public enum WorkOrderStatus
{
    Draft = 1,
    Open = 2,
    Assigned = 3,
    InProgress = 4,
    OnHold = 5,
    Completed = 6,
    Cancelled = 7
}
