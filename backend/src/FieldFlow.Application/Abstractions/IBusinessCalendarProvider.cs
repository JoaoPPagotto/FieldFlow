using FieldFlow.Domain.Calendar;

namespace FieldFlow.Application.Abstractions;

/// <summary>
/// Fornece o <see cref="SlaCalculator"/> configurado (fuso, expediente, dias úteis) com os feriados
/// da tabela <c>holidays</c>, mantidos em cache.
/// </summary>
public interface IBusinessCalendarProvider
{
    Task<SlaCalculator> GetSlaCalculatorAsync(CancellationToken cancellationToken = default);

    /// <summary>Descarta o cache (chamar após alterar feriados).</summary>
    void Invalidate();
}
