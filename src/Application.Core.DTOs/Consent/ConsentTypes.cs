namespace Application.Core.DTOs.Consent;

public static class ConsentTypes
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        "Research", "Marketing", "ThirdPartySharing", "Other"
    };
}
