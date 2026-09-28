using Bogus;
using Bogus.Extensions.Brazil;
using FieldFlow.Application.Abstractions;
using FieldFlow.Domain.Assets;
using FieldFlow.Domain.Calendar;
using FieldFlow.Domain.Catalog;
using FieldFlow.Domain.Common;
using FieldFlow.Domain.Customers;
using FieldFlow.Domain.Technicians;
using FieldFlow.Domain.WorkOrders;
using FieldFlow.Infrastructure.Calendar;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FieldFlow.Infrastructure.Persistence.Seed;

/// <summary>
/// Popula o banco com dados de demonstração: 3 clientes, 12 ativos, 5 técnicos, catálogo,
/// feriados nacionais e ~80 OS em estados variados — todas criadas <b>pelos métodos de domínio</b>,
/// então a timeline e o SLA são coerentes. Determinístico (seed fixa) e idempotente.
/// </summary>
public sealed class DemoDataSeeder
{
    public const int WorkOrderCount = 80;
    private const string SeedActor = "seed@fieldflow.dev";
    private static readonly Priority[] Priorities = [Priority.Low, Priority.Medium, Priority.High, Priority.Critical];
    private static readonly float[] PriorityWeights = [0.25f, 0.4f, 0.25f, 0.1f];
    private static readonly string[] Resolutions = ["Ajuste realizado e testado", "Peça substituída", "Limpeza e recalibração"];

    private readonly ApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;
    private readonly BusinessCalendarOptions _calendarOptions;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(ApplicationDbContext context, IDateTimeProvider clock,
                          IOptions<BusinessCalendarOptions> calendarOptions, ILogger<DemoDataSeeder> logger)
    {
        _context = context;
        _clock = clock;
        _calendarOptions = calendarOptions.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Customers.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Demo data already present — skipping seed.");
            return;
        }

        Randomizer.Seed = new Random(20260302);
        var faker = new Faker("pt_BR");

        var holidays = BuildHolidays();
        var serviceTypes = BuildServiceTypes();
        var parts = BuildParts();
        var customers = BuildCustomers(faker);
        var assets = BuildAssets(faker, customers);
        var technicians = BuildTechnicians(faker, serviceTypes);

        _context.Holidays.AddRange(holidays);
        _context.ServiceTypes.AddRange(serviceTypes);
        _context.Parts.AddRange(parts);
        _context.Customers.AddRange(customers);
        _context.Assets.AddRange(assets);
        _context.Technicians.AddRange(technicians);

        var calendar = new BusinessCalendar(
            TimeZoneInfo.FindSystemTimeZoneById(_calendarOptions.TimeZoneId),
            _calendarOptions.WorkdayStart,
            _calendarOptions.WorkdayEnd,
            WorkingDaysExtensions.FromDays(_calendarOptions.WorkingDays),
            holidays.Select(h => h.Date));
        var sla = new SlaCalculator(calendar);

        var workOrders = await BuildWorkOrdersAsync(faker, sla, customers, assets, serviceTypes, technicians, parts, cancellationToken);
        _context.WorkOrders.AddRange(workOrders);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Demo data seeded: {Customers} customers, {Assets} assets, {Technicians} technicians, {WorkOrders} work orders.",
            customers.Count, assets.Count, technicians.Count, workOrders.Count);
    }

    // ------------------------------------------------------------------ catálogos

    private static List<Holiday> BuildHolidays()
    {
        (int Y, int M, int D, string Name)[] dates =
        [
            (2026, 1, 1, "Confraternização Universal"), (2026, 4, 3, "Sexta-feira Santa"), (2026, 4, 21, "Tiradentes"),
            (2026, 5, 1, "Dia do Trabalho"), (2026, 6, 4, "Corpus Christi"), (2026, 9, 7, "Independência do Brasil"),
            (2026, 10, 12, "Nossa Senhora Aparecida"), (2026, 11, 2, "Finados"), (2026, 11, 15, "Proclamação da República"),
            (2026, 11, 20, "Dia da Consciência Negra"), (2026, 12, 25, "Natal"),
            (2027, 1, 1, "Confraternização Universal"), (2027, 3, 26, "Sexta-feira Santa"), (2027, 4, 21, "Tiradentes"),
            (2027, 5, 1, "Dia do Trabalho"), (2027, 5, 27, "Corpus Christi"), (2027, 9, 7, "Independência do Brasil"),
            (2027, 10, 12, "Nossa Senhora Aparecida"), (2027, 11, 2, "Finados"), (2027, 11, 15, "Proclamação da República"),
            (2027, 11, 20, "Dia da Consciência Negra"), (2027, 12, 25, "Natal")
        ];
        return dates.Select(d => Holiday.Create(new DateOnly(d.Y, d.M, d.D), d.Name).Value).ToList();
    }

    private static List<ServiceType> BuildServiceTypes()
    {
        (string Name, int Minutes, int Low, int Medium, int High, int Critical)[] data =
        [
            ("Manutenção preventiva", 240, 72, 48, 24, 8),
            ("Manutenção corretiva", 120, 48, 24, 8, 4),
            ("Instalação de equipamento", 480, 120, 72, 48, 24),
            ("Inspeção técnica", 60, 72, 48, 24, 8),
            ("Atendimento emergencial", 120, 24, 12, 4, 2)
        ];
        return data.Select(d => ServiceType.Create(d.Name, d.Minutes,
            SlaPolicy.Create(d.Low, d.Medium, d.High, d.Critical).Value).Value).ToList();
    }

    private static List<Part> BuildParts()
    {
        (string Code, string Name, decimal Price, int Stock)[] data =
        [
            ("FLT-001", "Filtro de ar G4", 89.90m, 120), ("FLT-002", "Filtro de ar F8", 159.00m, 60),
            ("CTR-010", "Contator tripolar 32A", 210.50m, 40), ("CAP-045", "Capacitor 45µF", 64.00m, 80),
            ("RLM-006", "Rolamento 6204", 38.70m, 150), ("COR-A42", "Correia em V A42", 52.30m, 90),
            ("GAS-410", "Fluido refrigerante R-410A (kg)", 95.00m, 200), ("SNS-TMP", "Sensor de temperatura NTC", 74.90m, 70),
            ("PLC-IO8", "Módulo de E/S 8 canais", 1290.00m, 12), ("VLV-EXP", "Válvula de expansão", 480.00m, 25),
            ("MTR-075", "Motor ventilador 0,75cv", 890.00m, 15), ("FUS-010", "Fusível NH 10A", 12.40m, 300)
        ];
        return data.Select(d => Part.Create(d.Code, d.Name, d.Price, d.Stock).Value).ToList();
    }

    private static List<Customer> BuildCustomers(Faker faker)
    {
        string[] names = ["Hospital Vale Verde", "Frigorífico Serra Azul", "Shopping Parque das Águas"];
        string[] domains = ["valeverde.example.com", "serraazul.example.com", "parquedasaguas.example.com"];

        return names.Select((name, i) =>
        {
            var address = Address.Create(
                faker.Address.StreetName(), faker.Address.BuildingNumber(), faker.Address.City(),
                faker.Address.StateAbbr(), faker.Address.ZipCode("#####-###")).Value;

            return Customer.Create(name, faker.Company.Cnpj(), $"manutencao@{domains[i]}",
                faker.Phone.PhoneNumber("(1#) 3###-####"), address).Value;
        }).ToList();
    }

    private static List<Asset> BuildAssets(Faker faker, List<Customer> customers)
    {
        (string Prefix, string Model, string Manufacturer)[] catalog =
        [
            ("CHL", "Chiller parafuso 120TR", "Refrigera Norte"), ("FCL", "Fancoil 5TR", "ClimaSul"),
            ("GER", "Gerador diesel 250kVA", "Motrix"), ("ELV", "Elevador de carga 2t", "Vertical Sistemas"),
            ("CMP", "Compressor de ar 40HP", "Pneutec"), ("CFR", "Câmara fria -18°C", "Refrigera Norte")
        ];

        var assets = new List<Asset>();
        foreach (var customer in customers)
        {
            for (var i = 1; i <= 4; i++)
            {
                var item = faker.PickRandom(catalog);
                var installed = DateOnly.FromDateTime(faker.Date.Past(4, new DateTime(2026, 1, 1)));
                var asset = Asset.Create(customer.CustomerId, $"{item.Prefix}-{i:D2}", item.Model, item.Manufacturer,
                    faker.Random.Replace("SN-####-??##").ToUpperInvariant(), installed, installed.AddYears(3)).Value;
                assets.Add(asset);
            }
        }

        // Um ativo desativado, para demonstrar a regra de abertura de OS
        assets[^1].Decommission();
        return assets;
    }

    private static List<Technician> BuildTechnicians(Faker faker, List<ServiceType> serviceTypes)
    {
        var technicians = new List<Technician>();
        for (var i = 0; i < 5; i++)
        {
            var technician = Technician.Create(faker.Name.FullName(), WorkingHours.Standard).Value;
            // Cada tipo de serviço fica coberto por pelo menos dois técnicos
            technician.AddSkill(serviceTypes[i % serviceTypes.Count].ServiceTypeId);
            technician.AddSkill(serviceTypes[(i + 1) % serviceTypes.Count].ServiceTypeId);
            if (i % 2 == 0) technician.AddSkill(serviceTypes[(i + 3) % serviceTypes.Count].ServiceTypeId);
            technicians.Add(technician);
        }
        return technicians;
    }

    // ------------------------------------------------------------------ ordens de serviço

    private async Task<List<WorkOrder>> BuildWorkOrdersAsync(
        Faker faker, SlaCalculator sla, List<Customer> customers, List<Asset> assets, List<ServiceType> serviceTypes,
        List<Technician> technicians, List<Part> parts, CancellationToken cancellationToken)
    {
        // Distribuição de estados-alvo (soma = 80)
        var targets = new List<WorkOrderStatus>();
        targets.AddRange(Enumerable.Repeat(WorkOrderStatus.Draft, 4));
        targets.AddRange(Enumerable.Repeat(WorkOrderStatus.Open, 10));
        targets.AddRange(Enumerable.Repeat(WorkOrderStatus.Assigned, 10));
        targets.AddRange(Enumerable.Repeat(WorkOrderStatus.InProgress, 8));
        targets.AddRange(Enumerable.Repeat(WorkOrderStatus.OnHold, 6));
        targets.AddRange(Enumerable.Repeat(WorkOrderStatus.Completed, 36));
        targets.AddRange(Enumerable.Repeat(WorkOrderStatus.Cancelled, 6));

        string[] problems =
        [
            "Equipamento não liga", "Ruído anormal no motor", "Vazamento de fluido", "Temperatura acima do setpoint",
            "Alarme de alta pressão", "Manutenção programada do trimestre", "Vibração excessiva", "Falha intermitente no painel",
            "Troca de filtros", "Inspeção pós-instalação"
        ];
        string[] holdReasons = ["Aguardando peça do fornecedor", "Cliente sem acesso à área", "Aguardando aprovação de orçamento"];
        string[] cancelReasons = ["OS duplicada", "Cliente cancelou o atendimento", "Equipamento será substituído"];

        var now = _clock.UtcNow;
        var usableAssets = assets.Where(a => a.Status != AssetStatus.Decommissioned).ToList();
        var agenda = technicians.ToDictionary(t => t.TechnicianId, _ => new List<(DateTime Start, DateTime End)>());
        var workOrders = new List<WorkOrder>(WorkOrderCount);

        foreach (var target in faker.Random.Shuffle(targets))
        {
            var asset = faker.PickRandom(usableAssets);
            var customer = customers.Single(c => c.CustomerId == asset.CustomerId);
            var serviceType = faker.PickRandom(serviceTypes);
            var priority = faker.Random.WeightedRandom(Priorities, PriorityWeights);

            // Em execução agora: início algumas horas atrás. Encerradas: 5–45 dias atrás. Demais: últimos dias.
            var isRunning = target is WorkOrderStatus.InProgress or WorkOrderStatus.OnHold;
            DateTime? runningStart = isRunning ? now.AddHours(-faker.Random.Int(2, 6)) : null;
            var createdAt = runningStart is { } rs
                ? rs.AddHours(-faker.Random.Int(4, 30))
                : BusinessMoment(sla, now.AddDays(-(target is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled
                    ? faker.Random.Int(5, 45)
                    : faker.Random.Int(1, 5))), faker);

            var number = WorkOrderNumber.From(createdAt.Year, await NextSequenceAsync(cancellationToken));
            var wo = WorkOrder.Create(number, customer.CustomerId, asset, serviceType, priority,
                faker.PickRandom(problems), SeedActor, createdAt).Value;
            wo.SetCreated(SeedActor, createdAt); // created_at coerente com a timeline
            workOrders.Add(wo);

            if (target == WorkOrderStatus.Draft) continue;

            var openedAt = createdAt.AddMinutes(faker.Random.Int(5, 40));
            Ensure(wo.Open(sla, SeedActor, openedAt));
            if (target == WorkOrderStatus.Open) continue;

            if (target == WorkOrderStatus.Cancelled && faker.Random.Bool())
            {
                Ensure(wo.Cancel(faker.PickRandom(cancelReasons), sla, SeedActor, openedAt.AddHours(2)));
                continue;
            }

            var duration = TimeSpan.FromMinutes(serviceType.DefaultDurationMinutes);
            var assignedAt = openedAt.AddMinutes(faker.Random.Int(10, 90));
            var candidates = technicians.Where(t => t.HasSkill(serviceType.ServiceTypeId)).ToList();

            // Agendadas (Assigned) ficam no futuro; as demais começam logo após a atribuição
            var desiredStart = target == WorkOrderStatus.Assigned
                ? BusinessMoment(sla, now.AddDays(faker.Random.Int(1, 7)), faker)
                : runningStart ?? sla.AddBusinessTime(assignedAt, TimeSpan.FromMinutes(faker.Random.Int(30, 180)));
            var (technician, start) = PickSlot(faker, sla, candidates, agenda, desiredStart, duration);
            if (isRunning && start > now.AddMinutes(-30)) start = runningStart!.Value; // em execução: sempre no passado
            var end = start + duration;
            agenda[technician.TechnicianId].Add((start, end));

            Ensure(wo.Assign(technician, start, end, SeedActor, assignedAt));

            if (target == WorkOrderStatus.Assigned) continue;
            if (target == WorkOrderStatus.Cancelled)
            {
                Ensure(wo.Cancel(faker.PickRandom(cancelReasons), sla, SeedActor, assignedAt.AddHours(1)));
                continue;
            }

            // Execução
            Ensure(wo.Start(technician.Name, start));
            wo.AddChecklistItem("Equipamento desenergizado antes da intervenção?", true, SeedActor, start);
            wo.AddChecklistItem("Área limpa e sinalizada ao final?", true, SeedActor, start);
            wo.AddChecklistItem("Observações para a próxima visita", false, SeedActor, start);

            var workedUntil = start + TimeSpan.FromMinutes(duration.TotalMinutes * faker.Random.Double(0.6, 1.3));
            if (isRunning)
                workedUntil = Min(workedUntil, now.AddMinutes(-5));

            if (workedUntil > start.AddMinutes(10))
                Ensure(wo.AddTimeEntry(technician.TechnicianId, start, workedUntil, null, technician.Name, workedUntil));

            if (faker.Random.Bool(0.6f))
            {
                var part = faker.PickRandom(parts);
                wo.AddUsedPart(part, faker.Random.Int(1, 3), technician.Name, workedUntil);
            }

            if (target == WorkOrderStatus.InProgress) continue;

            if (target == WorkOrderStatus.OnHold)
            {
                var holdAt = workedUntil > start ? workedUntil : start.AddMinutes(15);
                Ensure(wo.Hold(faker.PickRandom(holdReasons), technician.Name, holdAt));
                continue;
            }

            // Completed
            foreach (var item in wo.ChecklistItems.Where(i => i.IsRequired))
                Ensure(wo.AnswerChecklistItem(item.ChecklistItemId, "Sim", technician.Name, workedUntil));

            if (faker.Random.Bool(0.2f) && workedUntil < now.AddDays(-1))
            {
                // Algumas passaram por pausa antes de concluir
                Ensure(wo.Hold(faker.PickRandom(holdReasons), technician.Name, workedUntil));
                workedUntil = sla.AddBusinessTime(workedUntil, TimeSpan.FromHours(faker.Random.Int(1, 6)));
                Ensure(wo.Resume(sla, technician.Name, workedUntil));
            }

            Ensure(wo.Complete(faker.PickRandom(Resolutions),
                technician.Name, workedUntil.AddMinutes(faker.Random.Int(5, 30))));
        }

        return workOrders;
    }

    /// <summary>Um horário comercial "plausível" no dia informado (ou no próximo dia útil).</summary>
    private static DateTime BusinessMoment(SlaCalculator sla, DateTime aroundUtc, Faker faker)
    {
        var dayStart = sla.AddBusinessTime(aroundUtc.Date.AddHours(-12), TimeSpan.FromMinutes(1));
        return sla.AddBusinessTime(dayStart, TimeSpan.FromMinutes(faker.Random.Int(0, 7 * 60)));
    }

    /// <summary>Escolhe técnico apto e horário sem sobreposição na agenda já montada.</summary>
    private static (Technician Technician, DateTime Start) PickSlot(
        Faker faker, SlaCalculator sla, List<Technician> candidates,
        Dictionary<Guid, List<(DateTime Start, DateTime End)>> agenda, DateTime desiredStart, TimeSpan duration)
    {
        var start = desiredStart;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            foreach (var technician in faker.Random.Shuffle(candidates))
            {
                var end = start + duration;
                if (!agenda[technician.TechnicianId].Any(slot => start < slot.End && slot.Start < end))
                    return (technician, start);
            }
            start = sla.AddBusinessTime(start, TimeSpan.FromHours(1));
        }
        return (candidates[0], start);
    }

    private async Task<long> NextSequenceAsync(CancellationToken cancellationToken)
        => await _context.Database
            .SqlQueryRaw<long>("SELECT nextval('fieldflow.work_order_number_seq') AS \"Value\"")
            .SingleAsync(cancellationToken);

    private static void Ensure(Result result)
    {
        if (result.IsFailure)
            throw new InvalidOperationException($"Seed inconsistente: {result.Error.Code} — {result.Error.Message}");
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}
