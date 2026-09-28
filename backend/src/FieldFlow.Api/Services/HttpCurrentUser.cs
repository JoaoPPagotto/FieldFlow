using System.Security.Claims;
using FieldFlow.Application.Abstractions;

namespace FieldFlow.Api.Services;

/// <summary>Resolve o usuário atual a partir das claims do <see cref="HttpContext"/>.</summary>
internal sealed class HttpCurrentUser : ICurrentUser
{
    private const string SystemUser = "System";
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub"), out var id)
            ? id
            : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? Principal?.FindFirstValue("email");

    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);

    public string AuditName => IsAuthenticated ? Email ?? UserId?.ToString() ?? SystemUser : SystemUser;
}
