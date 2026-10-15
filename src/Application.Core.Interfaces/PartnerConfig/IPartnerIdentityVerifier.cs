namespace Application.Core.Interfaces.PartnerConfig;

public enum PartnerIdentityCheck
{
    /// <summary>The partner DB and the catalog registry name the same partner.</summary>
    Match,

    /// <summary>Both sides were read and they differ: this API is pointed at another partner's DB or catalog credential.</summary>
    Mismatch,

    /// <summary>One side could not be read (unreachable, not registered, no catalog configured), so nothing can be concluded.</summary>
    Unverified
}

/// <summary>Compares <c>PartnerConfig.PartnerCode</c> (partner DB) with the code <c>partner_self</c> returns (catalog).</summary>
public interface IPartnerIdentityVerifier
{
    Task<PartnerIdentityCheck> VerifyAsync();
}
