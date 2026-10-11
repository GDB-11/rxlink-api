using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;

namespace Infrastructure.Core.Services.Activity;

public sealed class RecordAccessLogger : IRecordAccessLogger
{
    private readonly IActivityLogger _activityLogger;
    private readonly IUserContext _userContext;

    public RecordAccessLogger(IActivityLogger activityLogger, IUserContext userContext)
    {
        _activityLogger = activityLogger;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public Task LogAsync(string resourceType, Guid patientCode, Guid? resourceCode = null,
        bool viaBreakGlass = false) =>
        _activityLogger.LogAsync(new ActivityEvent(
            ActivityEventTypes.RecordAccessed,
            _userContext.UserCode,
            ActivityEntityTypes.Patient,
            patientCode,
            $"Accessed {resourceType}",
            // ActorCode is kept in the payload because patient self-reads have no User row (ActorUserId is NULL).
            new
            {
                ResourceType = resourceType,
                ResourceCode = resourceCode,
                ActorCode = _userContext.UserCode,
                AccessBasis = viaBreakGlass ? "BreakGlass" : "Standard"
            }));
}
