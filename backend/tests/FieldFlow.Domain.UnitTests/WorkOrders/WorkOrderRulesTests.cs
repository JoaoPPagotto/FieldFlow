using FieldFlow.Domain.Assets;
using FieldFlow.Domain.UnitTests.TestData;
using FieldFlow.Domain.WorkOrders;
using static FieldFlow.Domain.UnitTests.TestData.DomainFixtures;

namespace FieldFlow.Domain.UnitTests.WorkOrders;

public class WorkOrderRulesTests
{
    // ---------------------------------------------------------------- criação

    [Fact]
    public void Create_fails_when_asset_belongs_to_another_customer()
    {
        var s = new Scenario();

        var result = WorkOrder.Create(WorkOrderNumber.From(2026, 2), Guid.NewGuid(), s.Asset, s.ServiceType,
            Priority.Low, "Ruído", Actor, MondayAt(9));

        result.Error.Code.Should().Be("WORK_ORDER_ASSET_CUSTOMER_MISMATCH");
    }

    [Fact]
    public void Create_fails_for_decommissioned_asset()
    {
        var s = new Scenario();
        s.Asset.Decommission();

        var result = WorkOrder.Create(WorkOrderNumber.From(2026, 2), s.Customer.CustomerId, s.Asset, s.ServiceType,
            Priority.Low, "Ruído", Actor, MondayAt(9));

        result.Error.Code.Should().Be("WORK_ORDER_ASSET_DECOMMISSIONED");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_requires_description(string? description)
    {
        var s = new Scenario();

        var result = WorkOrder.Create(WorkOrderNumber.From(2026, 2), s.Customer.CustomerId, s.Asset, s.ServiceType,
            Priority.Low, description, Actor, MondayAt(9));

        result.Error.Code.Should().Be("WORK_ORDER_DESCRIPTION_REQUIRED");
    }

    // ---------------------------------------------------------------- atribuição

    [Fact]
    public void Assign_fails_when_technician_lacks_the_service_type_skill()
    {
        var s = new Scenario().In(WorkOrderStatus.Open);
        var unskilled = Technician();

        var result = s.WorkOrder.Assign(unskilled, MondayAt(10), MondayAt(11), Actor, MondayAt(9));

        result.Error.Code.Should().Be("WORK_ORDER_TECHNICIAN_LACKS_SKILL");
        s.WorkOrder.AssignedTechnicianId.Should().BeNull();
    }

    [Fact]
    public void Assign_fails_when_window_ends_before_it_starts()
    {
        var s = new Scenario().In(WorkOrderStatus.Open);

        var result = s.WorkOrder.Assign(s.Technician, MondayAt(11), MondayAt(10), Actor, MondayAt(9));

        result.Error.Code.Should().Be("WORK_ORDER_SCHEDULE_INVALID");
    }

    [Fact]
    public void Reassign_keeps_status_and_records_previous_technician()
    {
        var s = new Scenario().In(WorkOrderStatus.Assigned);
        var other = Technician(s.ServiceType);

        var result = s.WorkOrder.Assign(other, MondayAt(14), MondayAt(16), Actor, MondayAt(9, 30));

        result.IsSuccess.Should().BeTrue();
        s.WorkOrder.AssignedTechnicianId.Should().Be(other.TechnicianId);
        s.WorkOrder.ScheduledStart.Should().Be(MondayAt(14));
        s.WorkOrder.Events.Last().Payload.Should().Contain(s.Technician.TechnicianId.ToString());
    }

    // ---------------------------------------------------------------- pausa e SLA

    [Fact]
    public void Hold_and_resume_extend_due_date_by_paused_business_time()
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress); // aberta seg 09:00, High = 8h → vence seg 17:00
        s.WorkOrder.DueAt.Should().Be(MondayAt(17));

        s.WorkOrder.Hold("Aguardando peça", Actor, MondayAt(11));
        s.WorkOrder.Resume(s.Sla, Actor, MondayAt(13));

        // 2h comerciais pausadas: seg 17:00 + 2h = 1h na segunda + 1h na terça
        s.WorkOrder.TotalHoldDuration.Should().Be(TimeSpan.FromHours(2));
        s.WorkOrder.DueAt.Should().Be(At(Monday.AddDays(1), 9));
        s.WorkOrder.HoldStartedAt.Should().BeNull();
    }

    [Fact]
    public void Overnight_hold_only_discounts_business_hours_from_sla()
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);

        s.WorkOrder.Hold("Cliente ausente", Actor, MondayAt(16));
        s.WorkOrder.Resume(s.Sla, Actor, At(Monday.AddDays(1), 10)); // 18h de relógio, 4h comerciais

        s.WorkOrder.TotalHoldDuration.Should().Be(TimeSpan.FromHours(18));
        s.WorkOrder.DueAt.Should().Be(At(Monday.AddDays(1), 11)); // seg 17:00 + 4h comerciais
    }

    [Fact]
    public void Sla_clock_is_frozen_while_on_hold()
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);
        s.WorkOrder.Hold("Aguardando peça", Actor, MondayAt(11));

        var muchLater = At(Monday.AddDays(5), 12);

        s.WorkOrder.GetSlaStatus(s.Sla, muchLater).Should().Be(SlaStatus.OnTime);
    }

    [Fact]
    public void Sla_status_is_null_before_opening_and_breached_after_due()
    {
        var draft = new Scenario();
        draft.WorkOrder.GetSlaStatus(draft.Sla, MondayAt(10)).Should().BeNull();

        var open = new Scenario().In(WorkOrderStatus.Open);
        open.WorkOrder.GetSlaStatus(open.Sla, At(Monday.AddDays(1), 10)).Should().Be(SlaStatus.Breached);
    }

    [Fact]
    public void Cancelling_while_on_hold_closes_the_pause()
    {
        var s = new Scenario().In(WorkOrderStatus.OnHold); // pausa desde 11:00

        s.WorkOrder.Cancel("Equipamento será substituído", s.Sla, Actor, MondayAt(15));

        s.WorkOrder.Status.Should().Be(WorkOrderStatus.Cancelled);
        s.WorkOrder.TotalHoldDuration.Should().Be(TimeSpan.FromHours(4));
        s.WorkOrder.CancellationReason.Should().Be("Equipamento será substituído");
    }

    // ---------------------------------------------------------------- apontamentos

    [Fact]
    public void AddTimeEntry_rejects_overlap_for_same_technician()
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);
        s.WorkOrder.AddTimeEntry(s.Technician.TechnicianId, MondayAt(10), MondayAt(11), null, Actor, MondayAt(12));

        var result = s.WorkOrder.AddTimeEntry(s.Technician.TechnicianId, MondayAt(10, 30), MondayAt(11, 30), null, Actor, MondayAt(12));

        result.Error.Code.Should().Be("WORK_ORDER_TIME_ENTRY_OVERLAP");
        s.WorkOrder.TimeEntries.Should().HaveCount(1);
    }

    [Fact]
    public void AddTimeEntry_accepts_adjacent_entries()
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);
        s.WorkOrder.AddTimeEntry(s.Technician.TechnicianId, MondayAt(10), MondayAt(11), null, Actor, MondayAt(12));

        var result = s.WorkOrder.AddTimeEntry(s.Technician.TechnicianId, MondayAt(11), MondayAt(12), "Testes", Actor, MondayAt(12));

        result.IsSuccess.Should().BeTrue();
        s.WorkOrder.TotalWorkedTime.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void AddTimeEntry_only_for_assigned_technician()
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);

        var result = s.WorkOrder.AddTimeEntry(Guid.NewGuid(), MondayAt(10), MondayAt(11), null, Actor, MondayAt(12));

        result.Error.Code.Should().Be("WORK_ORDER_TECHNICIAN_NOT_ASSIGNED");
    }

    [Fact]
    public void AddTimeEntry_rejects_future_and_inverted_intervals()
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);
        var tech = s.Technician.TechnicianId;

        s.WorkOrder.AddTimeEntry(tech, MondayAt(10), MondayAt(15), null, Actor, MondayAt(12))
            .Error.Code.Should().Be("WORK_ORDER_TIME_ENTRY_IN_FUTURE");
        s.WorkOrder.AddTimeEntry(tech, MondayAt(11), MondayAt(10), null, Actor, MondayAt(12))
            .Error.Code.Should().Be("WORK_ORDER_TIME_ENTRY_INVALID");
    }

    [Fact]
    public void AddTimeEntry_is_not_allowed_before_start()
    {
        var s = new Scenario().In(WorkOrderStatus.Assigned);

        var result = s.WorkOrder.AddTimeEntry(s.Technician.TechnicianId, MondayAt(9), MondayAt(9, 10), null, Actor, MondayAt(9, 20));

        result.Error.Code.Should().Be("WORK_ORDER_OPERATION_NOT_ALLOWED");
    }

    // ---------------------------------------------------------------- peças, checklist, anexos

    [Fact]
    public void AddUsedPart_decreases_stock_and_freezes_price()
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);
        var part = Part(stock: 5);

        var result = s.WorkOrder.AddUsedPart(part, 2, Actor, MondayAt(11));

        result.IsSuccess.Should().BeTrue();
        part.StockQuantity.Should().Be(3);
        result.Value.UnitPrice.Should().Be(89.90m);
        result.Value.Total.Should().Be(179.80m);
    }

    [Fact]
    public void AddUsedPart_fails_on_insufficient_stock_without_side_effects()
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);
        var part = Part(stock: 1);

        var result = s.WorkOrder.AddUsedPart(part, 2, Actor, MondayAt(11));

        result.Error.Code.Should().Be("PART_INSUFFICIENT_STOCK");
        part.StockQuantity.Should().Be(1);
        s.WorkOrder.UsedParts.Should().BeEmpty();
    }

    [Fact]
    public void AnswerChecklistItem_fails_for_unknown_item_or_empty_answer()
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);
        var item = s.WorkOrder.AddChecklistItem("Tensão ok?", true, Actor, MondayAt(10)).Value;

        s.WorkOrder.AnswerChecklistItem(Guid.NewGuid(), "Sim", Actor, MondayAt(11))
            .Error.Code.Should().Be("WORK_ORDER_CHECKLIST_ITEM_NOT_FOUND");
        s.WorkOrder.AnswerChecklistItem(item.ChecklistItemId, " ", Actor, MondayAt(11))
            .Error.Code.Should().Be("WORK_ORDER_CHECKLIST_ANSWER_REQUIRED");
        item.IsAnswered.Should().BeFalse();
    }

    [Theory]
    [InlineData("foto.jpg", "image/jpeg", 0L)]
    [InlineData("foto.jpg", "image/jpeg", 11L * 1024 * 1024)]
    [InlineData("", "image/jpeg", 1000L)]
    [InlineData("foto.jpg", "", 1000L)]
    public void AddAttachment_validates_metadata(string fileName, string contentType, long size)
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);

        var result = s.WorkOrder.AddAttachment(fileName, contentType, size, "work-orders/x/foto.jpg", Actor, MondayAt(11));

        result.Error.Code.Should().Be("WORK_ORDER_ATTACHMENT_INVALID");
    }

    [Fact]
    public void Field_operations_append_to_timeline()
    {
        var s = new Scenario().In(WorkOrderStatus.InProgress);
        var before = s.WorkOrder.Events.Count;

        s.WorkOrder.AddChecklistItem("Tensão ok?", true, Actor, MondayAt(10));
        s.WorkOrder.AddUsedPart(Part(), 1, Actor, MondayAt(10));
        s.WorkOrder.AddAttachment("antes.jpg", "image/jpeg", 2048, "k1", Actor, MondayAt(10));

        s.WorkOrder.Events.Should().HaveCount(before + 3);
        s.WorkOrder.Events.Skip(before).Select(e => e.Type).Should().Equal(
            WorkOrderEventType.ChecklistItemAdded, WorkOrderEventType.PartUsed, WorkOrderEventType.AttachmentAdded);
    }

    [Fact]
    public void Number_is_formatted_with_year_and_six_digit_sequence()
    {
        WorkOrderNumber.From(2026, 123).Value.Should().Be("OS-2026-000123");
        WorkOrderNumber.Parse("OS-2026-000123").IsSuccess.Should().BeTrue();
        WorkOrderNumber.Parse("OS-26-1").IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Asset_under_maintenance_can_still_receive_work_orders()
    {
        var s = new Scenario();
        s.Asset.MarkUnderMaintenance();

        var result = WorkOrder.Create(WorkOrderNumber.From(2026, 9), s.Customer.CustomerId, s.Asset, s.ServiceType,
            Priority.Medium, "Revisão", Actor, MondayAt(9));

        result.IsSuccess.Should().BeTrue();
        s.Asset.Status.Should().Be(AssetStatus.UnderMaintenance);
    }
}
