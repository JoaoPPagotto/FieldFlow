using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.WorkOrders;

public static class WorkOrderErrors
{
    public static Error InvalidTransition(WorkOrderStatus from, WorkOrderStatus to)
        => Error.Conflict("WORK_ORDER_INVALID_TRANSITION", $"Transição inválida: {from} → {to}.");

    public static Error Immutable(WorkOrderStatus status)
        => Error.Conflict("WORK_ORDER_IMMUTABLE", $"OS {status} não pode mais ser alterada.");

    public static Error OperationNotAllowed(string operation, WorkOrderStatus status)
        => Error.Conflict("WORK_ORDER_OPERATION_NOT_ALLOWED", $"Operação '{operation}' não permitida com a OS em {status}.");

    public static Error RequiredChecklistPending(int count)
        => Error.BusinessRule("WORK_ORDER_REQUIRED_CHECKLIST_PENDING",
            $"Existem {count} item(ns) obrigatório(s) do checklist sem resposta.");

    public static Error TimeEntryOverlap(DateTime start, DateTime end)
        => Error.Conflict("WORK_ORDER_TIME_ENTRY_OVERLAP",
            $"Apontamento sobrepõe outro já existente ({start:yyyy-MM-dd HH:mm}–{end:HH:mm} UTC).");

    public static readonly Error DescriptionRequired = Error.Validation("WORK_ORDER_DESCRIPTION_REQUIRED", "Descrição da OS é obrigatória.");
    public static readonly Error DescriptionTooLong = Error.Validation("WORK_ORDER_DESCRIPTION_MAX_LENGTH_EXCEEDED", "Descrição da OS deve ter no máximo 2000 caracteres.");
    public static readonly Error ReasonRequired = Error.Validation("WORK_ORDER_REASON_REQUIRED", "Justificativa é obrigatória.");
    public static readonly Error ReasonTooLong = Error.Validation("WORK_ORDER_REASON_MAX_LENGTH_EXCEEDED", "Justificativa deve ter no máximo 500 caracteres.");
    public static readonly Error AssetFromAnotherCustomer = Error.Validation("WORK_ORDER_ASSET_CUSTOMER_MISMATCH", "O ativo não pertence ao cliente informado.");
    public static readonly Error AssetDecommissioned = Error.BusinessRule("WORK_ORDER_ASSET_DECOMMISSIONED", "Não é possível abrir OS para ativo desativado.");
    public static readonly Error InvalidSchedule = Error.Validation("WORK_ORDER_SCHEDULE_INVALID", "O fim do agendamento deve ser posterior ao início.");
    public static readonly Error TechnicianLacksSkill = Error.BusinessRule("WORK_ORDER_TECHNICIAN_LACKS_SKILL", "O técnico não possui habilidade para este tipo de serviço.");
    public static readonly Error TechnicianRequired = Error.BusinessRule("WORK_ORDER_TECHNICIAN_REQUIRED", "É necessário um técnico atribuído para concluir a OS.");
    public static readonly Error TimeEntryRequired = Error.BusinessRule("WORK_ORDER_TIME_ENTRY_REQUIRED", "É necessário ao menos um apontamento de horas para concluir a OS.");
    public static readonly Error InvalidTimeEntry = Error.Validation("WORK_ORDER_TIME_ENTRY_INVALID", "O fim do apontamento deve ser posterior ao início.");
    public static readonly Error TimeEntryInFuture = Error.Validation("WORK_ORDER_TIME_ENTRY_IN_FUTURE", "Apontamento não pode terminar no futuro.");
    public static readonly Error TechnicianNotAssigned = Error.Forbidden("WORK_ORDER_TECHNICIAN_NOT_ASSIGNED", "Somente o técnico atribuído pode apontar nesta OS.");
    public static readonly Error ChecklistQuestionRequired = Error.Validation("WORK_ORDER_CHECKLIST_QUESTION_REQUIRED", "Pergunta do checklist é obrigatória.");
    public static readonly Error ChecklistAnswerRequired = Error.Validation("WORK_ORDER_CHECKLIST_ANSWER_REQUIRED", "Resposta do checklist é obrigatória.");
    public static readonly Error ChecklistItemNotFound = Error.NotFound("WORK_ORDER_CHECKLIST_ITEM_NOT_FOUND", "Item de checklist não encontrado.");
    public static readonly Error InvalidAttachment = Error.Validation("WORK_ORDER_ATTACHMENT_INVALID", "Anexo inválido (nome, tipo, tamanho até 10 MB e chave de armazenamento são obrigatórios).");
    public static readonly Error NotFound = Error.NotFound("WORK_ORDER_NOT_FOUND", "Ordem de serviço não encontrada.");
}
