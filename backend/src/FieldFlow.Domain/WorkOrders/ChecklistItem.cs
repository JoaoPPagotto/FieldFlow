using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.WorkOrders;

/// <summary>Pergunta do checklist da OS. Itens obrigatórios precisam de resposta para concluir.</summary>
public sealed class ChecklistItem : BaseEntity
{
    private ChecklistItem() { } // EF Core

    internal ChecklistItem(Guid workOrderId, string question, bool isRequired, int order)
    {
        ChecklistItemId = Guid.NewGuid();
        WorkOrderId = workOrderId;
        Question = question;
        IsRequired = isRequired;
        Order = order;
    }

    public Guid ChecklistItemId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public string Question { get; private set; } = null!;
    public bool IsRequired { get; private set; }
    public int Order { get; private set; }
    public string? Answer { get; private set; }
    public DateTime? AnsweredAt { get; private set; }
    public string? AnsweredBy { get; private set; }

    public bool IsAnswered => !string.IsNullOrWhiteSpace(Answer);

    internal void Respond(string answer, string answeredBy, DateTime answeredAtUtc)
    {
        Answer = answer;
        AnsweredBy = answeredBy;
        AnsweredAt = answeredAtUtc;
    }
}
