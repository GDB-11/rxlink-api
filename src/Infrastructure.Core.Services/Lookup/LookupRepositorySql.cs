namespace Infrastructure.Core.Services.Lookup;

internal static class LookupRepositorySql
{
    internal const string GetPharmaceuticalForms = """
                                                   SELECT "PharmaceuticalFormId" AS "Id", "Name"
                                                   FROM   "PharmaceuticalForm"
                                                   ORDER  BY "PharmaceuticalFormId"
                                                   """;

    internal const string GetAdministrationRoutes = """
                                                    SELECT "AdministrationRouteId" AS "Id", "Name"
                                                    FROM   "AdministrationRoute"
                                                    ORDER  BY "AdministrationRouteId"
                                                    """;

    internal const string GetSexes = """
                                     SELECT "SexCode" AS "Code", "Name"
                                     FROM   "Sex"
                                     ORDER  BY "Name"
                                     """;

    internal const string GetActiveDocumentTypes = """
                                                   SELECT "DocumentTypeCode" AS "Code", "Name"
                                                   FROM   "DocumentType"
                                                   WHERE  "IsActive" = TRUE
                                                   ORDER  BY "Name"
                                                   """;

    internal const string GetActiveRoles = """
                                           SELECT "RoleCode" AS "Code", "Name"
                                           FROM   "Role"
                                           WHERE  "IsActive" = TRUE
                                           ORDER  BY "Name"
                                           """;

    internal const string GetActiveSpecialties = """
                                                 SELECT "SpecialtyCode" AS "Code", "Name"
                                                 FROM   "Specialty"
                                                 WHERE  "IsActive" = TRUE
                                                 ORDER  BY "Name"
                                                 """;

    internal const string GetAllergySeverities = """
                                                 SELECT "SeverityCode" AS "Code", "Name"
                                                 FROM   "AllergySeverity"
                                                 WHERE  "IsActive" = TRUE
                                                 ORDER  BY "SortOrder"
                                                 """;

    internal const string GetActivePrescriptionStatuses = """
                                                          SELECT "PrescriptionStatusCode" AS "Code", "Name"
                                                          FROM   "PrescriptionStatus"
                                                          WHERE  "IsActive" = TRUE
                                                          ORDER  BY "PrescriptionStatusId"
                                                          """;

    internal const string GetActiveMedications = """
                                                 SELECT
                                                     m."MedicationCode" AS "Code",
                                                     CASE
                                                         WHEN m."CommercialName" IS NOT NULL
                                                             THEN m."CommercialName" || ' - ' || m."GenericName"
                                                         ELSE m."GenericName"
                                                     END AS "Name",
                                                     m."Concentration"           AS "DefaultDose",
                                                     ar."AdministrationRouteCode" AS "DefaultAdministrationRouteCode"
                                                 FROM  "Medication" m
                                                 JOIN  "AdministrationRoute" ar
                                                       ON m."AdministrationRouteId" = ar."AdministrationRouteId"
                                                 WHERE m."IsActive"   = TRUE
                                                   AND m."DeletedAt" IS NULL
                                                 ORDER BY m."GenericName"
                                                 """;

    internal const string GetActiveAdministrationRoutes = """
                                                          SELECT "AdministrationRouteCode" AS "Code", "Name"
                                                          FROM   "AdministrationRoute"
                                                          WHERE  "IsActive" = TRUE
                                                          ORDER  BY "Name"
                                                          """;

    internal const string GetActiveFrequencies = """
                                                 SELECT "FrequencyCode" AS "Code", "Description" AS "Name"
                                                 FROM   "Frequency"
                                                 WHERE  "IsActive" = TRUE
                                                 ORDER  BY "IntervalHours"
                                                 """;

    internal const string GetActiveConsultationTypes = """
                                                       SELECT "ConsultationTypeCode" AS "Code", "Name"
                                                       FROM   "ConsultationType"
                                                       WHERE  "IsActive" = TRUE
                                                       ORDER  BY "ConsultationTypeId"
                                                       """;
}