using FieldFlow.Domain.Common;

namespace FieldFlow.Domain.Calendar;

/// <summary>Feriado: dia inteiro fora do horário comercial para efeito de SLA.</summary>
public sealed class Holiday : AggregateRoot
{
    private Holiday() { } // EF Core

    public Guid HolidayId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Name { get; private set; } = null!;

    public static Result<Holiday> Create(DateOnly date, string? name)
    {
        var validation = Guard.Required(name, 100,
            Error.Validation("HOLIDAY_NAME_REQUIRED", "Nome do feriado é obrigatório."),
            Error.Validation("HOLIDAY_NAME_MAX_LENGTH_EXCEEDED", "Nome do feriado deve ter no máximo 100 caracteres."));
        if (validation.IsFailure) return validation.Error;

        return new Holiday { HolidayId = Guid.NewGuid(), Date = date, Name = name!.Trim() };
    }
}
