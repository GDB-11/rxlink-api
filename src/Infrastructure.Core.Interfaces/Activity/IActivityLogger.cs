using System.Data;

namespace Infrastructure.Core.Interfaces.Activity;

public interface IActivityLogger
{
    /// <summary>
    /// Records the event in its own transaction. For events with no business write to enlist in
    /// (auth events); use it from service pipelines via <c>TapAsync</c>/<c>TapErrorAsync</c>.
    /// </summary>
    Task LogAsync(ActivityEvent activityEvent);

    /// <summary>
    /// Records the event inside an existing transaction, so the event exists if and only if the
    /// business change commits. Use it from repositories that own the transaction.
    /// </summary>
    Task LogAsync(IDbConnection connection, IDbTransaction transaction, ActivityEvent activityEvent);
}
