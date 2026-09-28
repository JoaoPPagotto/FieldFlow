using FieldFlow.Domain.Assets;
using FieldFlow.Domain.Calendar;
using FieldFlow.Domain.Catalog;
using FieldFlow.Domain.Customers;
using FieldFlow.Domain.Technicians;
using FieldFlow.Domain.WorkOrders;
using AddressVo = FieldFlow.Domain.Customers.Address;
using AssetEntity = FieldFlow.Domain.Assets.Asset;
using CustomerEntity = FieldFlow.Domain.Customers.Customer;
using PartEntity = FieldFlow.Domain.Catalog.Part;
using ServiceTypeEntity = FieldFlow.Domain.Catalog.ServiceType;
using TechnicianEntity = FieldFlow.Domain.Technicians.Technician;
using WorkOrderEntity = FieldFlow.Domain.WorkOrders.WorkOrder;

namespace FieldFlow.Domain.UnitTests.TestData;

/// <summary>Builders para os testes. Datas em UTC com calendário UTC (seg–sex, 08–18).</summary>
internal static class DomainFixtures
{
    public const string Actor = "dispatcher@fieldflow.dev";

    /// <summary>Segunda-feira, 02/03/2026.</summary>
    public static readonly DateOnly Monday = new(2026, 3, 2);

    public static DateTime At(DateOnly day, int hour, int minute = 0)
        => day.ToDateTime(new TimeOnly(hour, minute), DateTimeKind.Utc);

    public static DateTime MondayAt(int hour, int minute = 0) => At(Monday, hour, minute);

    public static SlaCalculator Sla(params DateOnly[] holidays)
        => new(BusinessCalendar.Default(TimeZoneInfo.Utc, holidays));

    public static Address Address()
        => AddressVo.Create("Av. Paulista", "1000", "São Paulo", "SP", "01310-100").Value;

    public static Customer Customer()
        => CustomerEntity.Create("Acme Refrigeração Ltda", "11.222.333/0001-81", "contato@acme.com.br",
            "(11) 3333-4444", Address()).Value;

    public static Asset Asset(Guid customerId)
        => AssetEntity.Create(customerId, "chl-01", "Chiller 30TR", "Carrier", "SN-123",
            new DateOnly(2024, 1, 10), new DateOnly(2027, 1, 10)).Value;

    public static ServiceType ServiceType()
        => ServiceTypeEntity.Create("Manutenção corretiva", 120, SlaPolicy.Create(48, 24, 8, 4).Value).Value;

    public static Technician Technician(ServiceType? skill = null)
    {
        var technician = TechnicianEntity.Create("Carla Souza", WorkingHours.Standard).Value;
        if (skill is not null) technician.AddSkill(skill.ServiceTypeId);
        return technician;
    }

    public static Part Part(int stock = 10)
        => PartEntity.Create("flt-001", "Filtro de ar", 89.90m, stock).Value;

    /// <summary>Cenário padrão: OS em Draft com técnico apto disponível.</summary>
    public sealed class Scenario
    {
        public Scenario(Priority priority = Priority.High)
        {
            Customer = DomainFixtures.Customer();
            Asset = DomainFixtures.Asset(Customer.CustomerId);
            ServiceType = DomainFixtures.ServiceType();
            Technician = DomainFixtures.Technician(ServiceType);
            WorkOrder = WorkOrderEntity.Create(WorkOrderNumber.From(2026, 1), Customer.CustomerId, Asset,
                ServiceType, priority, "Chiller não liga", Actor, MondayAt(8, 30)).Value;
        }

        public Customer Customer { get; }
        public Asset Asset { get; }
        public ServiceType ServiceType { get; }
        public Technician Technician { get; }
        public WorkOrder WorkOrder { get; }
        public SlaCalculator Sla { get; } = DomainFixtures.Sla();

        /// <summary>Leva a OS até o estado pedido usando apenas métodos de domínio.</summary>
        public Scenario In(WorkOrderStatus status)
        {
            if (status == WorkOrderStatus.Draft) return this;

            WorkOrder.Open(Sla, Actor, MondayAt(9)).IsSuccess.Should().BeTrue();
            if (status == WorkOrderStatus.Open) return this;

            if (status == WorkOrderStatus.Cancelled)
            {
                WorkOrder.Cancel("Cliente desistiu", Sla, Actor, MondayAt(9, 30)).IsSuccess.Should().BeTrue();
                return this;
            }

            WorkOrder.Assign(Technician, MondayAt(10), MondayAt(12), Actor, MondayAt(9, 15)).IsSuccess.Should().BeTrue();
            if (status == WorkOrderStatus.Assigned) return this;

            WorkOrder.Start(Actor, MondayAt(10)).IsSuccess.Should().BeTrue();
            if (status == WorkOrderStatus.InProgress) return this;

            if (status == WorkOrderStatus.OnHold)
            {
                WorkOrder.Hold("Aguardando peça", Actor, MondayAt(11)).IsSuccess.Should().BeTrue();
                return this;
            }

            WorkOrder.AddTimeEntry(Technician.TechnicianId, MondayAt(10), MondayAt(11), null, Actor, MondayAt(11))
                .IsSuccess.Should().BeTrue();
            WorkOrder.Complete("Troca do contator", Actor, MondayAt(11, 30)).IsSuccess.Should().BeTrue();
            return this;
        }
    }
}
