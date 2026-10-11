using System.Data;
using System.Text.Json;
using Dapper;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;

namespace Infrastructure.Core.Services.Activity;

public sealed class ActivityLogger : BaseDatabaseService, IActivityLogger
{
    private const string InsertSql = """
                                     INSERT INTO "ActivityEvent"
                                         ("EventType", "ActorUserId", "EntityType", "EntityCode", "Summary", "Payload", "IpAddress", "RequestId")
                                     VALUES (
                                         @EventType,
                                         (SELECT "UserId" FROM "User" WHERE "UserCode" = @ActorUserCode),
                                         @EntityType,
                                         @EntityCode,
                                         @Summary,
                                         @Payload::jsonb,
                                         NULLIF(@Ip, '')::inet,
                                         NULLIF(@RequestId, '')
                                     )
                                     """;

    private readonly IDbConnection _connection;
    private readonly IUserContext _userContext;

    public ActivityLogger(IDbConnection connection, IUserContext userContext)
    {
        _connection = connection;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public Task LogAsync(ActivityEvent activityEvent) =>
        ExecuteWithTransactionAsync(_connection, transaction => InsertAsync(_connection, transaction, activityEvent));

    /// <inheritdoc/>
    public Task LogAsync(IDbConnection connection, IDbTransaction transaction, ActivityEvent activityEvent) =>
        InsertAsync(connection, transaction, activityEvent);

    private Task<int> InsertAsync(IDbConnection connection, IDbTransaction transaction, ActivityEvent e) =>
        connection.ExecuteAsync(
            InsertSql,
            new
            {
                e.EventType,
                e.ActorUserCode,
                e.EntityType,
                e.EntityCode,
                e.Summary,
                Payload = e.Payload is null ? null : JsonSerializer.Serialize(e.Payload),
                Ip = _userContext.IpAddress ?? string.Empty,
                RequestId = _userContext.RequestId ?? string.Empty
            },
            transaction);
}
