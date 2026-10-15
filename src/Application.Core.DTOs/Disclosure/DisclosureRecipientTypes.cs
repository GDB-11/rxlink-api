namespace Application.Core.DTOs.Disclosure;

public static class DisclosureRecipientTypes
{
    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        "Authority", "Court", "PublicHealth", "Insurer", "Family", "Researcher", "Other"
    };
}
