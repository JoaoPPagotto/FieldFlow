namespace FieldFlow.Domain.Technicians;

/// <summary>Habilidade do técnico = tipo de serviço que ele está apto a executar.</summary>
public sealed class TechnicianSkill
{
    private TechnicianSkill() { } // EF Core

    internal TechnicianSkill(Guid technicianId, Guid serviceTypeId)
    {
        TechnicianId = technicianId;
        ServiceTypeId = serviceTypeId;
    }

    public Guid TechnicianId { get; private set; }
    public Guid ServiceTypeId { get; private set; }
}
