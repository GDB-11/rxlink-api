namespace Infrastructure.Core.Interfaces.Audit;

/// <summary>
/// Request-scoped identity of the caller, used to stamp audited database writes.
/// </summary>
public interface IUserContext
{
    /// <summary>The <c>sub</c> claim of the JWT, or <c>null</c> for anonymous/system callers.</summary>
    Guid? UserCode { get; }

    /// <summary>Remote IP address of the request, if available.</summary>
    string? IpAddress { get; }

    /// <summary>Correlation id of the request (<c>HttpContext.TraceIdentifier</c>).</summary>
    string? RequestId { get; }
}
