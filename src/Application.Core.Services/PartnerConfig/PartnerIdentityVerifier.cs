using Application.Core.Interfaces.PartnerConfig;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.PartnerConfig;

namespace Application.Core.Services.PartnerConfig;

public sealed class PartnerIdentityVerifier : IPartnerIdentityVerifier
{
    private readonly IPartnerConfigRepository _config;
    private readonly IPartnerRegistryReader _registry;

    public PartnerIdentityVerifier(IPartnerConfigRepository config, IPartnerRegistryReader registry)
    {
        _config = config;
        _registry = registry;
    }

    /// <inheritdoc/>
    public async Task<PartnerIdentityCheck> VerifyAsync()
    {
        var local = await _config.GetPartnerCodeAsync();
        var remote = await _registry.GetIdentityAsync();

        if (!local.IsSuccess || !remote.IsSuccess || local.Value is null || remote.Value is null)
            return PartnerIdentityCheck.Unverified;

        return local.Value == remote.Value.PartnerCode ? PartnerIdentityCheck.Match : PartnerIdentityCheck.Mismatch;
    }
}
