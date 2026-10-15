using Infrastructure.Core.Interfaces.Audit;

namespace RxLinkApi.Http;

/// <summary>
/// <see cref="IUserContext"/> for work no user triggered (background jobs). There is no actor, and the request id carries
/// a <see cref="RequestIdPrefix"/> marker so the unattributed-writes report can tell system writes from missing context.
/// </summary>
internal sealed class SystemUserContext(string jobName) : IUserContext
{
    internal const string RequestIdPrefix = "system:";

    public Guid? UserCode => null;
    public string? Role => null;
    public string? IpAddress => null;
    public string? RequestId { get; } = RequestIdPrefix + jobName;
}
