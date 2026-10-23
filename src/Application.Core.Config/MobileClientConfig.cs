namespace Application.Core.Config;

/// <summary>What the mobile app is told at startup (<c>GET /api/v1/partner/config/public</c>).</summary>
public sealed record MobileClientConfig
{
    /// <summary>Oldest app version this deployment still supports; older apps must ask the user to update.</summary>
    public string MinimumAppVersion { get; init; } = "1.0.0";

    /// <summary>Optional modules this deployment switches on or off for the app. Empty until a module needs one.</summary>
    public IReadOnlyDictionary<string, bool> Features { get; init; } = new Dictionary<string, bool>();
}
