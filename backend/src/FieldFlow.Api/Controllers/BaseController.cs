using FieldFlow.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace FieldFlow.Api.Controllers;

/// <summary>
/// Base de todos os controllers OData. Rotas vêm do EDM (<c>ODataExtensions.GetEdmModel</c>),
/// por isso as actions padrão não levam template e o parâmetro de chave chama-se <c>key</c>.
/// Falhas são lançadas como <c>AppException</c> e viram Problem Details no middleware.
/// </summary>
public abstract class BaseController : ODataController
{
    private IMediator? _mediator;
    private ICurrentUser? _currentUser;

    protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();
    protected ICurrentUser CurrentUser => _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

    /// <summary>Identificador gravado em <c>command.UserId</c> antes de todo <c>Mediator.Send</c>.</summary>
    protected string GetCurrentUserId() => CurrentUser.AuditName;

    protected ActionResult HandleResult<T>(T? result)
        => result is null ? NotFound() : Ok(result);
}
