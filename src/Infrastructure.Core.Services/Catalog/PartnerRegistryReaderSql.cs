namespace Infrastructure.Core.Services.Catalog;

/// <summary>SQL used exclusively by <see cref="PartnerRegistryReader"/>; the views already filter on the connecting role.</summary>
internal static class PartnerRegistryReaderSql
{
    internal const string Identity = """
                                     SELECT "PartnerCode", "Slug", "DisplayName", "Country", "Timezone",
                                            "Currency", "Locale", "BrandPrimaryColor", "BrandAccentColor"
                                     FROM partner_self
                                     """;

    internal const string Logo = """
                                 SELECT "ContentType", "Data", "Sha256"
                                 FROM partner_logo_self
                                 """;
}
