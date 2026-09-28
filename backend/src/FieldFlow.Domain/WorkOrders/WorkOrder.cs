using System.Text.Json;
using FieldFlow.Domain.Assets;
using FieldFlow.Domain.Calendar;
using FieldFlow.Domain.Catalog;
using FieldFlow.Domain.Common;
using FieldFlow.Domain.Technicians;

namespace FieldFlow.Domain.WorkOrders;

/// <summary>
/// Ordem de serviço — agregado raiz principal e dono da máquina de estados.
/// <para>
/// Regras garantidas pela própria entidade (nenhum controller/handler altera estado diretamente):
/// </para>
/// <list type="number">
/// <item>Toda transição passa por um método de domínio; o setter de <see cref="Status"/> é privado.</item>
/// <item>Transição inválida → <see cref="WorkOrderErrors.InvalidTransition"/>.</item>
/// <item><see cref="Complete"/> exige técnico, ao menos um apontamento e checklist obrigatório respondido.</item>
/// <item><see cref="Cancel"/> e <see cref="Hold"/> exigem justificativa.</item>
/// <item>Toda operação bem-sucedida gera um <see cref="WorkOrderEvent"/> na timeline.</item>
/// <item>OS <c>Completed</c>/<c>Cancelled</c> é imutável.</item>
/// </list>
/// </summary>
public sealed class WorkOrder : AggregateRoot
{
    public const int DescriptionMaxLength = 2000;
    public const int ReasonMaxLength = 500;
    public const long MaxAttachmentBytes = 10 * 1024 * 1024;

    private static readonly Dictionary<WorkOrderStatus, WorkOrderStatus[]> AllowedTransitions = new()
    {
        [WorkOrderStatus.Draft] = [WorkOrderStatus.Open],
        [WorkOrderStatus.Open] = [WorkOrderStatus.Assigned, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.Assigned] = [WorkOrderStatus.Assigned, WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.InProgress] = [WorkOrderStatus.OnHold, WorkOrderStatus.Completed],
        [WorkOrderStatus.OnHold] = [WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.Completed] = [],
        [WorkOrderStatus.Cancelled] = []
    };

    private readonly List<TimeEntry> _timeEntries = [];
    private readonly List<UsedPart> _usedParts = [];
    private readonly List<ChecklistItem> _checklistItems = [];
    private readonly List<Attachment> _attachments = [];
    private readonly List<WorkOrderEvent> _events = [];

    private WorkOrder() { } // EF Core

    public Guid WorkOrderId { get; private set; }
    public WorkOrderNumber Number { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public Guid AssetId { get; private set; }
    public Guid ServiceTypeId { get; private set; }
    public Priority Priority { get; private set; }
    public WorkOrderStatus Status { get; private set; }
    public string Description { get; private set; } = null!;

    /// <summary>Horas de SLA congeladas na criação (tipo de serviço × prioridade).</summary>
    public int SlaHours { get; private set; }

    public DateTime? OpenedAt { get; private set; }
    /// <summary>Prazo calculado pelo <see cref="SlaCalculator"/>; estendido pelo tempo comercial em pausa.</summary>
    public DateTime? DueAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public Guid? AssignedTechnicianId { get; private set; }
    public DateTime? ScheduledStart { get; private set; }
    public DateTime? ScheduledEnd { get; private set; }

    public DateTime? HoldStartedAt { get; private set; }
    /// <summary>Tempo total (relógio) em <c>OnHold</c>.</summary>
    public TimeSpan TotalHoldDuration { get; private set; }

    public string? HoldReason { get; private set; }
    public string? CancellationReason { get; private set; }
    public string? ResolutionNotes { get; private set; }

    public IReadOnlyCollection<TimeEntry> TimeEntries => _timeEntries.AsReadOnly();
    public IReadOnlyCollection<UsedPart> UsedParts => _usedParts.AsReadOnly();
    public IReadOnlyCollection<ChecklistItem> ChecklistItems => _checklistItems.AsReadOnly();
    public IReadOnlyCollection<Attachment> Attachments => _attachments.AsReadOnly();
    public IReadOnlyCollection<WorkOrderEvent> Events => _events.AsReadOnly();

    public bool IsTerminal => Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled;

    public TimeSpan TotalWorkedTime => _timeEntries.Aggregate(TimeSpan.Zero, (acc, t) => acc + t.Duration);

    // ------------------------------------------------------------------ criação

    public static Result<WorkOrder> Create(
        WorkOrderNumber number,
        Guid customerId,
        Asset asset,
        ServiceType serviceType,
        Priority priority,
        string? description,
        string actor,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(serviceType);

        var validation = Guard.Required(description, DescriptionMaxLength,
            WorkOrderErrors.DescriptionRequired, WorkOrderErrors.DescriptionTooLong);
        if (validation.IsFailure) return validation.Error;
        if (asset.CustomerId != customerId) return WorkOrderErrors.AssetFromAnotherCustomer;
        if (asset.Status == AssetStatus.Decommissioned) return WorkOrderErrors.AssetDecommissioned;

        var workOrder = new WorkOrder
        {
            WorkOrderId = Guid.NewGuid(),
            Number = number,
            CustomerId = customerId,
            AssetId = asset.AssetId,
            ServiceTypeId = serviceType.ServiceTypeId,
            Priority = priority,
            Status = WorkOrderStatus.Draft,
            Description = description!.Trim(),
            SlaHours = serviceType.Sla.GetHours(priority)
        };

        workOrder.Record(WorkOrderEventType.Created, actor, nowUtc, null, WorkOrderStatus.Draft,
            new { number = number.Value, priority = priority.ToString(), workOrder.SlaHours });

        return workOrder;
    }

    // ------------------------------------------------------------------ transições

    /// <summary>Draft → Open. Calcula o <see cref="DueAt"/> em horário comercial.</summary>
    public Result Open(SlaCalculator slaCalculator, string actor, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(slaCalculator);

        var check = EnsureCanTransitionTo(WorkOrderStatus.Open);
        if (check.IsFailure) return check;

        OpenedAt = nowUtc;
        DueAt = slaCalculator.CalculateDueAt(nowUtc, SlaHours);

        return TransitionTo(WorkOrderStatus.Open, WorkOrderEventType.Opened, actor, nowUtc, new { dueAt = DueAt });
    }

    /// <summary>
    /// Open/Assigned → Assigned. Valida janela e habilidade do técnico. Conflito de agenda e jornada
    /// dependem de outras OS/fuso e são validados no handler (Fase 5), antes de chamar este método.
    /// </summary>
    public Result Assign(Technician technician, DateTime scheduledStartUtc, DateTime scheduledEndUtc,
                         string actor, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(technician);

        var check = EnsureCanTransitionTo(WorkOrderStatus.Assigned);
        if (check.IsFailure) return check;
        if (scheduledEndUtc <= scheduledStartUtc) return WorkOrderErrors.InvalidSchedule;
        if (!technician.HasSkill(ServiceTypeId)) return WorkOrderErrors.TechnicianLacksSkill;

        var previousTechnician = AssignedTechnicianId;
        AssignedTechnicianId = technician.TechnicianId;
        ScheduledStart = scheduledStartUtc;
        ScheduledEnd = scheduledEndUtc;

        return TransitionTo(WorkOrderStatus.Assigned, WorkOrderEventType.Assigned, actor, nowUtc, new
        {
            technicianId = technician.TechnicianId,
            technicianName = technician.Name,
            previousTechnicianId = previousTechnician,
            scheduledStart = scheduledStartUtc,
            scheduledEnd = scheduledEndUtc
        });
    }

    /// <summary>Assigned → InProgress.</summary>
    public Result Start(string actor, DateTime nowUtc)
    {
        var check = EnsureCanTransitionTo(WorkOrderStatus.InProgress);
        if (check.IsFailure) return check;
        if (Status == WorkOrderStatus.OnHold) return WorkOrderErrors.InvalidTransition(Status, WorkOrderStatus.InProgress);

        StartedAt ??= nowUtc;
        return TransitionTo(WorkOrderStatus.InProgress, WorkOrderEventType.Started, actor, nowUtc, null);
    }

    /// <summary>InProgress → OnHold. O relógio do SLA para enquanto a OS está em pausa.</summary>
    public Result Hold(string? reason, string actor, DateTime nowUtc)
    {
        var check = EnsureCanTransitionTo(WorkOrderStatus.OnHold);
        if (check.IsFailure) return check;

        var reasonCheck = ValidateReason(reason);
        if (reasonCheck.IsFailure) return reasonCheck;

        HoldStartedAt = nowUtc;
        HoldReason = reason!.Trim();
        return TransitionTo(WorkOrderStatus.OnHold, WorkOrderEventType.Held, actor, nowUtc, new { reason = HoldReason });
    }

    /// <summary>OnHold → InProgress. Soma a pausa e empurra o prazo pelo tempo comercial pausado.</summary>
    public Result Resume(SlaCalculator slaCalculator, string actor, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(slaCalculator);

        if (IsTerminal) return WorkOrderErrors.Immutable(Status);
        if (Status != WorkOrderStatus.OnHold) return WorkOrderErrors.InvalidTransition(Status, WorkOrderStatus.InProgress);

        var pausedBusinessTime = EndHold(slaCalculator, nowUtc);
        return TransitionTo(WorkOrderStatus.InProgress, WorkOrderEventType.Resumed, actor, nowUtc, new
        {
            pausedBusinessMinutes = Math.Round(pausedBusinessTime.TotalMinutes),
            dueAt = DueAt
        });
    }

    /// <summary>InProgress → Completed. Pré-condições: técnico, apontamento e checklist obrigatório.</summary>
    public Result Complete(string? resolutionNotes, string actor, DateTime nowUtc)
    {
        var check = EnsureCanTransitionTo(WorkOrderStatus.Completed);
        if (check.IsFailure) return check;

        if (AssignedTechnicianId is null) return WorkOrderErrors.TechnicianRequired;
        if (_timeEntries.Count == 0) return WorkOrderErrors.TimeEntryRequired;

        var pending = _checklistItems.Count(i => i.IsRequired && !i.IsAnswered);
        if (pending > 0) return WorkOrderErrors.RequiredChecklistPending(pending);

        ClosedAt = nowUtc;
        ResolutionNotes = string.IsNullOrWhiteSpace(resolutionNotes) ? null : resolutionNotes.Trim();
        return TransitionTo(WorkOrderStatus.Completed, WorkOrderEventType.Completed, actor, nowUtc, new
        {
            workedMinutes = Math.Round(TotalWorkedTime.TotalMinutes),
            resolution = ResolutionNotes
        });
    }

    /// <summary>Open/Assigned/OnHold → Cancelled. Exige justificativa.</summary>
    public Result Cancel(string? reason, SlaCalculator slaCalculator, string actor, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(slaCalculator);

        var check = EnsureCanTransitionTo(WorkOrderStatus.Cancelled);
        if (check.IsFailure) return check;

        var reasonCheck = ValidateReason(reason);
        if (reasonCheck.IsFailure) return reasonCheck;

        if (Status == WorkOrderStatus.OnHold) EndHold(slaCalculator, nowUtc);

        ClosedAt = nowUtc;
        CancellationReason = reason!.Trim();
        return TransitionTo(WorkOrderStatus.Cancelled, WorkOrderEventType.Cancelled, actor, nowUtc,
            new { reason = CancellationReason });
    }

    // ------------------------------------------------------------------ execução em campo

    /// <summary>Apontamento de horas: só o técnico atribuído, sem sobreposição e nunca no futuro.</summary>
    public Result<TimeEntry> AddTimeEntry(Guid technicianId, DateTime startedAtUtc, DateTime endedAtUtc,
                                          string? notes, string actor, DateTime nowUtc)
    {
        var check = EnsureExecuting(nameof(AddTimeEntry));
        if (check.IsFailure) return check.Error;
        if (AssignedTechnicianId != technicianId) return WorkOrderErrors.TechnicianNotAssigned;
        if (endedAtUtc <= startedAtUtc) return WorkOrderErrors.InvalidTimeEntry;
        if (endedAtUtc > nowUtc) return WorkOrderErrors.TimeEntryInFuture;

        var overlapping = _timeEntries.FirstOrDefault(t => t.TechnicianId == technicianId && t.Overlaps(startedAtUtc, endedAtUtc));
        if (overlapping is not null) return WorkOrderErrors.TimeEntryOverlap(overlapping.StartedAt, overlapping.EndedAt);

        var entry = new TimeEntry(WorkOrderId, technicianId, startedAtUtc, endedAtUtc,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());
        _timeEntries.Add(entry);

        Record(WorkOrderEventType.TimeEntryAdded, actor, nowUtc, null, null, new
        {
            timeEntryId = entry.TimeEntryId,
            startedAt = startedAtUtc,
            endedAt = endedAtUtc,
            minutes = Math.Round(entry.Duration.TotalMinutes)
        });
        return entry;
    }

    /// <summary>Registra peça usada e dá baixa no estoque (mesma transação).</summary>
    public Result<UsedPart> AddUsedPart(Part part, int quantity, string actor, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(part);

        var check = EnsureExecuting(nameof(AddUsedPart));
        if (check.IsFailure) return check.Error;

        var stock = part.DecreaseStock(quantity);
        if (stock.IsFailure) return stock.Error;

        var used = new UsedPart(WorkOrderId, part.PartId, part.Code, quantity, part.UnitPrice);
        _usedParts.Add(used);

        Record(WorkOrderEventType.PartUsed, actor, nowUtc, null, null,
            new { partId = part.PartId, code = part.Code, quantity, unitPrice = part.UnitPrice });
        return used;
    }

    public Result<ChecklistItem> AddChecklistItem(string? question, bool isRequired, string actor, DateTime nowUtc)
    {
        if (IsTerminal) return WorkOrderErrors.Immutable(Status);
        if (string.IsNullOrWhiteSpace(question)) return WorkOrderErrors.ChecklistQuestionRequired;

        var item = new ChecklistItem(WorkOrderId, question.Trim(), isRequired, _checklistItems.Count + 1);
        _checklistItems.Add(item);

        Record(WorkOrderEventType.ChecklistItemAdded, actor, nowUtc, null, null,
            new { checklistItemId = item.ChecklistItemId, question = item.Question, isRequired });
        return item;
    }

    public Result AnswerChecklistItem(Guid checklistItemId, string? answer, string actor, DateTime nowUtc)
    {
        var check = EnsureExecuting(nameof(AnswerChecklistItem));
        if (check.IsFailure) return check;

        var item = _checklistItems.FirstOrDefault(i => i.ChecklistItemId == checklistItemId);
        if (item is null) return WorkOrderErrors.ChecklistItemNotFound;
        if (string.IsNullOrWhiteSpace(answer)) return WorkOrderErrors.ChecklistAnswerRequired;

        item.Respond(answer.Trim(), actor, nowUtc);
        Record(WorkOrderEventType.ChecklistItemAnswered, actor, nowUtc, null, null,
            new { checklistItemId, answer = item.Answer });
        return Result.Success();
    }

    public Result<Attachment> AddAttachment(string? fileName, string? contentType, long sizeBytes, string? storageKey,
                                            string actor, DateTime nowUtc)
    {
        if (IsTerminal) return WorkOrderErrors.Immutable(Status);
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(contentType)
            || string.IsNullOrWhiteSpace(storageKey) || sizeBytes <= 0 || sizeBytes > MaxAttachmentBytes)
            return WorkOrderErrors.InvalidAttachment;

        var attachment = new Attachment(WorkOrderId, fileName.Trim(), contentType.Trim(), sizeBytes, storageKey, actor, nowUtc);
        _attachments.Add(attachment);

        Record(WorkOrderEventType.AttachmentAdded, actor, nowUtc, null, null,
            new { attachmentId = attachment.AttachmentId, fileName = attachment.FileName, sizeBytes });
        return attachment;
    }

    // ------------------------------------------------------------------ SLA (derivado)

    /// <summary>
    /// Situação do SLA em <paramref name="nowUtc"/>. Nulo enquanto a OS não foi aberta.
    /// Em pausa, o relógio fica congelado no início da pausa; encerrada, no fechamento.
    /// </summary>
    public SlaStatus? GetSlaStatus(SlaCalculator slaCalculator, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(slaCalculator);
        if (OpenedAt is not { } openedAt || DueAt is not { } dueAt) return null;

        var reference = Status switch
        {
            WorkOrderStatus.OnHold => HoldStartedAt ?? nowUtc,
            WorkOrderStatus.Completed or WorkOrderStatus.Cancelled => ClosedAt ?? nowUtc,
            _ => nowUtc
        };

        return slaCalculator.Evaluate(openedAt, dueAt, reference);
    }

    public static bool CanTransition(WorkOrderStatus from, WorkOrderStatus to)
        => AllowedTransitions.TryGetValue(from, out var targets) && targets.Contains(to);

    // ------------------------------------------------------------------ internos

    private Result EnsureCanTransitionTo(WorkOrderStatus target)
    {
        if (IsTerminal) return WorkOrderErrors.Immutable(Status);
        return CanTransition(Status, target)
            ? Result.Success()
            : WorkOrderErrors.InvalidTransition(Status, target);
    }

    private Result EnsureExecuting(string operation)
    {
        if (IsTerminal) return WorkOrderErrors.Immutable(Status);
        return Status is WorkOrderStatus.InProgress or WorkOrderStatus.OnHold
            ? Result.Success()
            : WorkOrderErrors.OperationNotAllowed(operation, Status);
    }

    private static Result ValidateReason(string? reason)
        => Guard.Required(reason, ReasonMaxLength, WorkOrderErrors.ReasonRequired, WorkOrderErrors.ReasonTooLong);

    private TimeSpan EndHold(SlaCalculator slaCalculator, DateTime nowUtc)
    {
        var holdStart = HoldStartedAt ?? nowUtc;
        var paused = slaCalculator.BusinessTimeBetween(holdStart, nowUtc);

        TotalHoldDuration += nowUtc - holdStart;
        if (DueAt is { } due) DueAt = slaCalculator.AddBusinessTime(due, paused);

        HoldStartedAt = null;
        HoldReason = null;
        return paused;
    }

    private Result TransitionTo(WorkOrderStatus target, WorkOrderEventType eventType, string actor, DateTime nowUtc, object? payload)
    {
        var from = Status;
        Status = target;
        Record(eventType, actor, nowUtc, from, target, payload);
        return Result.Success();
    }

    private void Record(WorkOrderEventType type, string actor, DateTime nowUtc,
                        WorkOrderStatus? from, WorkOrderStatus? to, object? payload)
    {
        var json = payload is null ? null : JsonSerializer.Serialize(payload);
        _events.Add(new WorkOrderEvent(WorkOrderId, type, string.IsNullOrWhiteSpace(actor) ? "System" : actor,
            nowUtc, from, to, json));
    }
}
