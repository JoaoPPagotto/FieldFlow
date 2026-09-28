namespace FieldFlow.Application.Abstractions;

/// <summary>Relógio abstraído — permite testes determinísticos. Sempre UTC.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
