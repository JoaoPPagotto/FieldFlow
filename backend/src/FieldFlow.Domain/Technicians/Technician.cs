using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.Technicians;

/// <summary>Técnico de campo: habilidades (tipos de serviço) e jornada de trabalho.</summary>
public sealed class Technician : AggregateRoot
{
    public const int NameMaxLength = 150;

    private readonly List<TechnicianSkill> _skills = [];

    private Technician() { } // EF Core

    public Guid TechnicianId { get; private set; }

    /// <summary>Usuário de login vinculado (Fase 2). Opcional.</summary>
    public Guid? UserId { get; private set; }

    public string Name { get; private set; } = null!;
    public WorkingHours WorkingHours { get; private set; } = null!;
    public IReadOnlyCollection<TechnicianSkill> Skills => _skills.AsReadOnly();

    public static Result<Technician> Create(string? name, WorkingHours workingHours, Guid? userId = null)
    {
        var validation = Guard.Required(name, NameMaxLength, TechnicianErrors.NameRequired, TechnicianErrors.NameTooLong);
        if (validation.IsFailure) return validation.Error;

        return new Technician
        {
            TechnicianId = Guid.NewGuid(),
            Name = name!.Trim(),
            WorkingHours = workingHours,
            UserId = userId
        };
    }

    public Result Rename(string? name)
    {
        var validation = Guard.Required(name, NameMaxLength, TechnicianErrors.NameRequired, TechnicianErrors.NameTooLong);
        if (validation.IsFailure) return validation;
        Name = name!.Trim();
        return Result.Success();
    }

    public void ChangeWorkingHours(WorkingHours workingHours) => WorkingHours = workingHours;

    public void LinkUser(Guid userId) => UserId = userId;

    public bool HasSkill(Guid serviceTypeId) => _skills.Any(s => s.ServiceTypeId == serviceTypeId);

    public Result AddSkill(Guid serviceTypeId)
    {
        if (serviceTypeId == Guid.Empty) return TechnicianErrors.SkillRequired;
        if (HasSkill(serviceTypeId)) return TechnicianErrors.SkillAlreadyAssigned;

        _skills.Add(new TechnicianSkill(TechnicianId, serviceTypeId));
        return Result.Success();
    }

    public Result RemoveSkill(Guid serviceTypeId)
    {
        var skill = _skills.FirstOrDefault(s => s.ServiceTypeId == serviceTypeId);
        if (skill is null) return TechnicianErrors.SkillNotAssigned;

        _skills.Remove(skill);
        return Result.Success();
    }

    /// <summary>A janela (UTC) cabe na jornada do técnico, no fuso informado?</summary>
    public bool IsWithinWorkingHours(DateTime startUtc, DateTime endUtc, TimeZoneInfo timeZone)
    {
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(startUtc, timeZone);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(endUtc, timeZone);
        return WorkingHours.Covers(localStart, localEnd);
    }
}
