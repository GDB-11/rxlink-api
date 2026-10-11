namespace Infrastructure.Core.Services.Catalog;

/// <summary>SQL used exclusively by <see cref="PartnerEnablementRepository"/>. Table names are fixed constants, never input.</summary>
internal static class PartnerEnablementRepositorySql
{
    internal const string EnabledMedications = """
                                               SELECT "MedicationCode" FROM "PartnerMedication"
                                               WHERE "IsEnabled" = TRUE AND "MedicationCode" = ANY(@Codes)
                                               """;

    internal const string EnabledSpecialties = """
                                               SELECT "SpecialtyCode" FROM "PartnerSpecialty"
                                               WHERE "IsEnabled" = TRUE AND "SpecialtyCode" = ANY(@Codes)
                                               """;

    internal const string EnabledInsurances = """
                                              SELECT "InsuranceCode" FROM "PartnerInsurance"
                                              WHERE "IsEnabled" = TRUE AND "InsuranceCode" = ANY(@Codes)
                                              """;
}
