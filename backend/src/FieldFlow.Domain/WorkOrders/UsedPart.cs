using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.WorkOrders;

/// <summary>Peça consumida na OS. O preço unitário é congelado no momento do uso.</summary>
public sealed class UsedPart : BaseEntity
{
    private UsedPart() { } // EF Core

    internal UsedPart(Guid workOrderId, Guid partId, string partCode, int quantity, decimal unitPrice)
    {
        UsedPartId = Guid.NewGuid();
        WorkOrderId = workOrderId;
        PartId = partId;
        PartCode = partCode;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid UsedPartId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public Guid PartId { get; private set; }
    public string PartCode { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal Total => Quantity * UnitPrice;
}
