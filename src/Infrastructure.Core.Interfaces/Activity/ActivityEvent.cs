namespace Infrastructure.Core.Interfaces.Activity;

/// <summary>A business event to record in <c>ActivityEvent</c>. Never put clinical content in it.</summary>
/// <param name="EventType">Catalog name, see <see cref="ActivityEventTypes"/>.</param>
/// <param name="ActorUserCode"><c>User.UserCode</c> of the actor, or <c>null</c> for anonymous/system/patient actors.</param>
/// <param name="EntityType">Kind of entity the event is about (e.g. <c>Prescription</c>).</param>
/// <param name="EntityCode">Public code of that entity.</param>
/// <param name="Summary">Short human-readable description.</param>
/// <param name="Payload">Optional extra data, serialized as JSONB. Codes and flags only.</param>
public sealed record ActivityEvent(
    string EventType,
    Guid? ActorUserCode,
    string? EntityType,
    Guid? EntityCode,
    string Summary,
    object? Payload = null);
