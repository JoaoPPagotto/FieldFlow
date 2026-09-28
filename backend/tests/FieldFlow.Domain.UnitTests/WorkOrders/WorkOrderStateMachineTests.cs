using FieldFlow.Domain.Common;
using FieldFlow.Domain.UnitTests.TestData;
using FieldFlow.Domain.WorkOrders;
using static FieldFlow.Domain.UnitTests.TestData.DomainFixtures;

namespace FieldFlow.Domain.UnitTests.WorkOrders;

public class WorkOrderStateMachineTests
{
    public enum Operation { Open, Assign, Start, Hold, Resume, Complete, Cancel }

    private static Result Execute(Scenario s, Operation operation) => operation switch
    {
        Operation.Open => s.WorkOrder.Open(s.Sla, Actor, MondayAt(13)),
        Operation.Assign => s.WorkOrder.Assign(s.Technician, MondayAt(14), MondayAt(15), Actor, MondayAt(13)),
        Operation.Start => s.WorkOrder.Start(Actor, MondayAt(13)),
        Operation.Hold => s.WorkOrder.Hold("Aguardando cliente", Actor, MondayAt(13)),
        Operation.Resume => s.WorkOrder.Resume(s.Sla, Actor, MondayAt(13)),
        Operation.Complete => s.WorkOrder.Complete(null, Actor, MondayAt(13)),
        Operation.Cancel => s.WorkOrder.Cancel("Duplicada", s.Sla, Actor, MondayAt(13)),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    [Fact]
    public void Create_starts_in_Draft_with_sla_hours_from_priority_and_a_Created_event()
    {
        var wo = new Scenario(Priority.Critical).WorkOrder;

        wo.Status.Should().Be(WorkOrderStatus.Draft);
        wo.SlaHours.Should().Be(4);
        wo.Number.Value.Should().Be("OS-2026-000001");
        wo.DueAt.Should().BeNull();
        wo.Events.Should().HaveCount(1);
        wo.Events.Single().Type.Should().Be(WorkOrderEventType.Created);
    }

    [Fact]
    public void Open_calculates_due_date_in_business_hours()
    {
        var s = new Scenario(Priority.High); // 8h

        var result = s.WorkOrder.Open(s.Sla, Actor, MondayAt(14));

        result.IsSuccess.Should().BeTrue();
        s.WorkOrder.Status.Should().Be(WorkOrderStatus.Open);
        s.WorkOrder.OpenedAt.Should().Be(MondayAt(14));
        s.WorkOrder.DueAt.Should().Be(At(Monday.AddDays(1), 12)); // 4h segunda + 4h terça
    }

    [Fact]
    public void Full_happy_path_reaches_Completed_and_records_the_timeline()
    {
        var s = new Scenario().In(WorkOrderStatus.Completed);

        s.WorkOrder.Status.Should().Be(WorkOrderStatus.Completed);
        s.WorkOrder.ClosedAt.Should().Be(MondayAt(11, 30));
        s.WorkOrder.ResolutionNotes.Should().Be("Troca do contator");
        s.WorkOrder.Events.Select(e => e.Type).Should().Equal(
            WorkOrderEventType.Created,
            WorkOrderEventType.Opened,
            WorkOrderEventType.Assigned,
            WorkOrderEventType.Started,
            WorkOrderEventType.TimeEntryAdded,
            WorkOrderEventType.Completed);
    }

    [Theory]
    [InlineData(WorkOrderStatus.Draft, Operation.Assign)]
    [InlineData(WorkOrderStatus.Draft, Operation.Start)]
    [InlineData(WorkOrderStatus.Draft, Operation.Hold)]
    [InlineData(WorkOrderStatus.Draft, Operation.Resume)]
    [InlineData(WorkOrderStatus.Draft, Operation.Complete)]
    [InlineData(WorkOrderStatus.Draft, Operation.Cancel)]
    [InlineData(WorkOrderStatus.Open, Operation.Open)]
    [InlineData(WorkOrderStatus.Open, Operation.Start)]
    [InlineData(WorkOrderStatus.Open, Operation.Hold)]
    [InlineData(WorkOrderStatus.Open, Operation.Resume)]
    [InlineData(WorkOrderStatus.Open, Operation.Complete)]
    [InlineData(WorkOrderStatus.Assigned, Operation.Open)]
    [InlineData(WorkOrderStatus.Assigned, Operation.Hold)]
    [InlineData(WorkOrderStatus.Assigned, Operation.Resume)]
    [InlineData(WorkOrderStatus.Assigned, Operation.Complete)]
    [InlineData(WorkOrderStatus.InProgress, Operation.Open)]
    [InlineData(WorkOrderStatus.InProgress, Operation.Assign)]
    [InlineData(WorkOrderStatus.InProgress, Operation.Start)]
    [InlineData(WorkOrderStatus.InProgress, Operation.Resume)]
    [InlineData(WorkOrderStatus.InProgress, Operation.Cancel)]
    [InlineData(WorkOrderStatus.OnHold, Operation.Open)]
    [InlineData(WorkOrderStatus.OnHold, Operation.Assign)]
    [InlineData(WorkOrderStatus.OnHold, Operation.Start)]
    [InlineData(WorkOrderStatus.OnHold, Operation.Hold)]
    [InlineData(WorkOrderStatus.OnHold, Operation.Complete)]
    public void Invalid_transition_fails_with_specific_error_and_keeps_state(WorkOrderStatus from, Operation operation)
    {
        var s = new Scenario().In(from);
        var eventsBefore = s.WorkOrder.Events.Count;

        var result = Execute(s, operation);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WORK_ORDER_INVALID_TRANSITION");
        result.Error.Type.Should().Be(ErrorType.Conflict);
        s.WorkOrder.Status.Should().Be(from);
        s.WorkOrder.Events.Should().HaveCount(eventsBefore);
    }

    [Theory]
    [InlineData(WorkOrderStatus.Completed, Operation.Open)]
    [InlineData(WorkOrderStatus.Completed, Operation.Assign)]
    [InlineData(WorkOrderStatus.Completed, Operation.Start)]
    [InlineData(WorkOrderStatus.Completed, Operation.Hold)]
    [InlineData(WorkOrderStatus.Completed, Operation.Resume)]
    [InlineData(WorkOrderStatus.Completed, Operation.Complete)]
    [InlineData(WorkOrderStatus.Completed, Operation.Cancel)]
    [InlineData(WorkOrderStatus.Cancelled, Operation.Open)]
    [InlineData(WorkOrderStatus.Cancelled, Operation.Assign)]
    [InlineData(WorkOrderStatus.Cancelled, Operation.Start)]
    [InlineData(WorkOrderStatus.Cancelled, Operation.Hold)]
    [InlineData(WorkOrderStatus.Cancelled, Operation.Resume)]
    [InlineData(WorkOrderStatus.Cancelled, Operation.Complete)]
    [InlineData(WorkOrderStatus.Cancelled, Operation.Cancel)]
    public void Terminal_work_order_is_immutable(WorkOrderStatus terminal, Operation operation)
    {
        var s = new Scenario().In(terminal);

        var result = Execute(s, operation);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("WORK_ORDER_IMMUTABLE");
        s.WorkOrder.Status.Should().Be(terminal);
    }

    [Theory]
    [InlineData(WorkOrderStatus.Completed)]
    [InlineData(WorkOrderStatus.Cancelled)]
    public void Terminal_work_order_rejects_field_operations(WorkOrderStatus terminal)
    {
        var s = new Scenario().In(terminal);

        s.WorkOrder.AddChecklistItem("Foto do painel?", true, Actor, MondayAt(13)).Error.Code.Should().Be("WORK_ORDER_IMMUTABLE");
        s.WorkOrder.AddAttachment("foto.jpg", "image/jpeg", 1000, "k", Actor, MondayAt(13)).Error.Code.Should().Be("WORK_ORDER_IMMUTABLE");
        s.WorkOrder.AddUsedPart(Part(), 1, Actor, MondayAt(13)).Error.Code.Should().Be("WORK_ORDER_IMMUTABLE");
    }

    [Theory]
    [InlineData(WorkOrderStatus.Open, Operation.Assign, WorkOrderStatus.Assigned)]
    [InlineData(WorkOrderStatus.Open, Operation.Cancel, WorkOrderStatus.Cancelled)]
    [InlineData(WorkOrderStatus.Assigned, Operation.Assign, WorkOrderStatus.Assigned)]
    [InlineData(WorkOrderStatus.Assigned, Operation.Start, WorkOrderStatus.InProgress)]
    [InlineData(WorkOrderStatus.Assigned, Operation.Cancel, WorkOrderStatus.Cancelled)]
    [InlineData(WorkOrderStatus.InProgress, Operation.Hold, WorkOrderStatus.OnHold)]
    [InlineData(WorkOrderStatus.OnHold, Operation.Resume, WorkOrderStatus.InProgress)]
    [InlineData(WorkOrderStatus.OnHold, Operation.Cancel, WorkOrderStatus.Cancelled)]
    public void Valid_transition_changes_status_and_records_event(WorkOrderStatus from, Operation operation, WorkOrderStatus to)
    {
        var s = new Scenario().In(from);

        var result = Execute(s, operation);

        result.IsSuccess.Should().BeTrue();
        s.WorkOrder.Status.Should().Be(to);
        var last = s.WorkOrder.Events.Last();
        last.FromStatus.Should().Be(from);
        last.ToStatus.Should().Be(to);
        last.Actor.Should().Be(Actor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Hold_requires_reason(string? reason)
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);

        var result = s.WorkOrder.Hold(reason, Actor, MondayAt(11));

        result.Error.Code.Should().Be("WORK_ORDER_REASON_REQUIRED");
        s.WorkOrder.Status.Should().Be(WorkOrderStatus.InProgress);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancel_requires_reason(string? reason)
    {
        var s = new Scenario().In(WorkOrderStatus.Open);

        var result = s.WorkOrder.Cancel(reason, s.Sla, Actor, MondayAt(11));

        result.Error.Code.Should().Be("WORK_ORDER_REASON_REQUIRED");
        s.WorkOrder.Status.Should().Be(WorkOrderStatus.Open);
    }

    [Fact]
    public void Complete_requires_at_least_one_time_entry()
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);

        var result = s.WorkOrder.Complete(null, Actor, MondayAt(12));

        result.Error.Code.Should().Be("WORK_ORDER_TIME_ENTRY_REQUIRED");
        result.Error.Type.Should().Be(ErrorType.BusinessRule);
        s.WorkOrder.Status.Should().Be(WorkOrderStatus.InProgress);
    }

    [Fact]
    public void Complete_requires_all_mandatory_checklist_items_answered()
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);
        s.WorkOrder.AddChecklistItem("Pressão verificada?", isRequired: true, Actor, MondayAt(10, 5));
        s.WorkOrder.AddChecklistItem("Área limpa?", isRequired: true, Actor, MondayAt(10, 5));
        s.WorkOrder.AddChecklistItem("Observações", isRequired: false, Actor, MondayAt(10, 5));
        s.WorkOrder.AddTimeEntry(s.Technician.TechnicianId, MondayAt(10), MondayAt(11), null, Actor, MondayAt(11));

        var result = s.WorkOrder.Complete(null, Actor, MondayAt(12));

        result.Error.Code.Should().Be("WORK_ORDER_REQUIRED_CHECKLIST_PENDING");
        result.Error.Message.Should().Contain("2");
    }

    [Fact]
    public void Complete_succeeds_when_mandatory_items_are_answered_even_if_optional_are_not()
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);
        var required = s.WorkOrder.AddChecklistItem("Pressão verificada?", true, Actor, MondayAt(10, 5)).Value;
        s.WorkOrder.AddChecklistItem("Observações", false, Actor, MondayAt(10, 5));
        s.WorkOrder.AddTimeEntry(s.Technician.TechnicianId, MondayAt(10), MondayAt(11), null, Actor, MondayAt(11));
        s.WorkOrder.AnswerChecklistItem(required.ChecklistItemId, "Sim, 8 bar", Actor, MondayAt(11));

        var result = s.WorkOrder.Complete(null, Actor, MondayAt(12));

        result.IsSuccess.Should().BeTrue();
        s.WorkOrder.Status.Should().Be(WorkOrderStatus.Completed);
    }

    [Fact]
    public void CanTransition_matches_the_documented_diagram()
    {
        WorkOrder.CanTransition(WorkOrderStatus.Draft, WorkOrderStatus.Open).Should().BeTrue();
        WorkOrder.CanTransition(WorkOrderStatus.InProgress, WorkOrderStatus.Completed).Should().BeTrue();
        WorkOrder.CanTransition(WorkOrderStatus.OnHold, WorkOrderStatus.Cancelled).Should().BeTrue();
        WorkOrder.CanTransition(WorkOrderStatus.Draft, WorkOrderStatus.Completed).Should().BeFalse();
        WorkOrder.CanTransition(WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled).Should().BeFalse();
        WorkOrder.CanTransition(WorkOrderStatus.Completed, WorkOrderStatus.Open).Should().BeFalse();
    }
}
