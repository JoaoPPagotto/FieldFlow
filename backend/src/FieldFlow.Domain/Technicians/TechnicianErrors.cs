using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.Technicians;

public static class TechnicianErrors
{
    public static readonly Error NameRequired = Error.Validation("TECHNICIAN_NAME_REQUIRED", "Nome do técnico é obrigatório.");
    public static readonly Error NameTooLong = Error.Validation("TECHNICIAN_NAME_MAX_LENGTH_EXCEEDED", "Nome do técnico deve ter no máximo 150 caracteres.");
    public static readonly Error InvalidWorkingHours = Error.Validation("TECHNICIAN_WORKING_HOURS_INVALID", "Fim da jornada deve ser posterior ao início.");
    public static readonly Error NoWorkingDays = Error.Validation("TECHNICIAN_WORKING_DAYS_REQUIRED", "Informe ao menos um dia de trabalho.");
    public static readonly Error SkillRequired = Error.Validation("TECHNICIAN_SKILL_REQUIRED", "Tipo de serviço da habilidade é obrigatório.");
    public static readonly Error SkillAlreadyAssigned = Error.Conflict("TECHNICIAN_SKILL_ALREADY_ASSIGNED", "O técnico já possui esta habilidade.");
    public static readonly Error SkillNotAssigned = Error.NotFound("TECHNICIAN_SKILL_NOT_ASSIGNED", "O técnico não possui esta habilidade.");
    public static readonly Error NotFound = Error.NotFound("TECHNICIAN_NOT_FOUND", "Técnico não encontrado.");
}
