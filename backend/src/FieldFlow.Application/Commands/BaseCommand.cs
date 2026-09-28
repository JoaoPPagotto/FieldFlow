using MediatR;

namespace FieldFlow.Application.Commands;

/// <summary>
/// Base de todo command. Padrão do projeto: um arquivo <c>{Entity}Command.cs</c> por agregado,
/// contendo os commands e seus handlers. O <see cref="UserId"/> é preenchido pelo controller.
/// </summary>
public abstract class BaseCommand : IRequest
{
    public string? UserId { get; set; }
}

public abstract class BaseCommand<TResponse> : IRequest<TResponse>
{
    public string? UserId { get; set; }
}
