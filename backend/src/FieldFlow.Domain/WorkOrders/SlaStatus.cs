namespace FieldFlow.Domain.WorkOrders;

/// <summary>Estado derivado do SLA — calculado na consulta, nunca persistido.</summary>
public enum SlaStatus
{
    OnTime = 1,
    AtRisk = 2,
    Breached = 3
}
