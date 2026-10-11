namespace Infrastructure.Core.Services.PartnerCatalog;

/// <summary>SQL used exclusively by <see cref="PartnerCatalogRepository"/>.</summary>
internal static class PartnerCatalogRepositorySql
{
    internal const string GetMedication = """
                                          SELECT @Code AS "MedicationCode",
                                                 COALESCE(pm."IsEnabled", FALSE) AS "IsEnabled",
                                                 cs."ReorderLevel"
                                          FROM (SELECT 1) one
                                          LEFT JOIN "PartnerMedication" pm ON pm."MedicationCode" = @Code
                                          LEFT JOIN "ClinicStock" cs ON cs."MedicationCode" = @Code
                                              AND cs."ClinicId" = (SELECT "ClinicId" FROM "Clinic" WHERE "IsDefault" = TRUE AND "IsActive" = TRUE)
                                          """;

    internal const string GetSpecialty = """
                                         SELECT @Code AS "SpecialtyCode",
                                                COALESCE(ps."IsEnabled", FALSE) AS "IsEnabled",
                                                COALESCE(ps."PriceInPerson", 0) AS "PriceInPerson",
                                                COALESCE(ps."PriceVirtual", 0) AS "PriceVirtual"
                                         FROM (SELECT 1) one
                                         LEFT JOIN "PartnerSpecialty" ps ON ps."SpecialtyCode" = @Code
                                         """;

    internal const string GetInsurance = """
                                         SELECT @Code AS "InsuranceCode",
                                                COALESCE(pi."IsEnabled", FALSE) AS "IsEnabled",
                                                pi."CoveragePercentageOverride"
                                         FROM (SELECT 1) one
                                         LEFT JOIN "PartnerInsurance" pi ON pi."InsuranceCode" = @Code
                                         """;

    internal const string GetAllMedications = """
                                              SELECT pm."MedicationCode", pm."IsEnabled", cs."ReorderLevel", cs."StockQuantity"
                                              FROM "PartnerMedication" pm
                                              LEFT JOIN "ClinicStock" cs ON cs."MedicationCode" = pm."MedicationCode"
                                                  AND cs."ClinicId" = (SELECT "ClinicId" FROM "Clinic" WHERE "IsDefault" = TRUE AND "IsActive" = TRUE)
                                              """;

    internal const string GetAllSpecialties = """
                                             SELECT "SpecialtyCode", "IsEnabled", "PriceInPerson", "PriceVirtual"
                                             FROM "PartnerSpecialty"
                                             """;

    internal const string GetAllInsurances = """
                                            SELECT "InsuranceCode", "IsEnabled", "CoveragePercentageOverride"
                                            FROM "PartnerInsurance"
                                            """;

    internal const string UpsertMedication = """
                                             INSERT INTO "PartnerMedication" ("MedicationCode", "IsEnabled", "UpdatedAt", "UpdatedBy")
                                             VALUES (@Code, @IsEnabled, NOW(), (SELECT "UserId" FROM "User" WHERE "UserCode" = @UserCode))
                                             ON CONFLICT ("MedicationCode") DO UPDATE
                                             SET "IsEnabled" = EXCLUDED."IsEnabled",
                                                 "UpdatedAt" = NOW(),
                                                 "UpdatedBy" = EXCLUDED."UpdatedBy"
                                             """;

    /// <summary>Only runs when a reorder level was sent; the stock row is created at zero if it does not exist yet.</summary>
    internal const string UpsertReorderLevel = """
                                               INSERT INTO "ClinicStock" ("ClinicId", "MedicationCode", "StockQuantity", "ReorderLevel", "UpdatedAt", "UpdatedBy")
                                               SELECT c."ClinicId", @Code, 0, @ReorderLevel, NOW(),
                                                      (SELECT "UserId" FROM "User" WHERE "UserCode" = @UserCode)
                                               FROM "Clinic" c
                                               WHERE c."IsDefault" = TRUE AND c."IsActive" = TRUE
                                               ON CONFLICT ("ClinicId", "MedicationCode") DO UPDATE
                                               SET "ReorderLevel" = EXCLUDED."ReorderLevel",
                                                   "UpdatedAt" = NOW(),
                                                   "UpdatedBy" = EXCLUDED."UpdatedBy"
                                               """;

    internal const string UpsertSpecialty = """
                                            INSERT INTO "PartnerSpecialty" ("SpecialtyCode", "IsEnabled", "PriceInPerson", "PriceVirtual", "UpdatedAt", "UpdatedBy")
                                            VALUES (@Code, @IsEnabled, @PriceInPerson, @PriceVirtual, NOW(),
                                                    (SELECT "UserId" FROM "User" WHERE "UserCode" = @UserCode))
                                            ON CONFLICT ("SpecialtyCode") DO UPDATE
                                            SET "IsEnabled" = EXCLUDED."IsEnabled",
                                                "PriceInPerson" = EXCLUDED."PriceInPerson",
                                                "PriceVirtual" = EXCLUDED."PriceVirtual",
                                                "UpdatedAt" = NOW(),
                                                "UpdatedBy" = EXCLUDED."UpdatedBy"
                                            """;

    internal const string UpsertInsurance = """
                                            INSERT INTO "PartnerInsurance" ("InsuranceCode", "IsEnabled", "CoveragePercentageOverride", "UpdatedAt", "UpdatedBy")
                                            VALUES (@Code, @IsEnabled, @CoveragePercentageOverride, NOW(),
                                                    (SELECT "UserId" FROM "User" WHERE "UserCode" = @UserCode))
                                            ON CONFLICT ("InsuranceCode") DO UPDATE
                                            SET "IsEnabled" = EXCLUDED."IsEnabled",
                                                "CoveragePercentageOverride" = EXCLUDED."CoveragePercentageOverride",
                                                "UpdatedAt" = NOW(),
                                                "UpdatedBy" = EXCLUDED."UpdatedBy"
                                            """;
}
