using System.Security.Claims;
using Infrastructure.Core.Interfaces.Audit;

namespace RxLinkApi.Http;

/// <summary>
/// <see cref="IUserContext"/> backed by the current <see cref="HttpContext"/>.
/// </summary>
internal sealed class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpUserContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? UserCode =>
        Guid.TryParse(_accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out Guid code)
            ? code
            : null;

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? RequestId => _accessor.HttpContext?.TraceIdentifier;
}
