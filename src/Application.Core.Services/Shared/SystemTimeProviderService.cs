using Application.Core.Interfaces.Shared;

namespace Application.Core.Services.Shared;

public sealed class SystemTimeProviderService : ITimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
}