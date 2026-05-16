namespace Application.Core.Interfaces.Shared;

public interface ITimeProvider
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
}