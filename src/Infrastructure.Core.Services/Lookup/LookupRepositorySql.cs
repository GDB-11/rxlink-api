namespace Infrastructure.Core.Services.Lookup;

internal static class LookupRepositorySql
{
    internal const string GetActiveRoles = """
                                           SELECT "RoleCode" AS "Code", "Name"
                                           FROM   "Role"
                                           WHERE  "IsActive" = TRUE
                                           ORDER  BY "Name"
                                           """;

    internal const string GetActivePrescriptionStatuses = """
                                                          SELECT "PrescriptionStatusCode" AS "Code", "Name"
                                                          FROM   "PrescriptionStatus"
                                                          WHERE  "IsActive" = TRUE
                                                          ORDER  BY "PrescriptionStatusId"
                                                          """;

    internal const string GetActiveConsultationTypes = """
                                                       SELECT "ConsultationTypeCode" AS "Code", "Name"
                                                       FROM   "ConsultationType"
                                                       WHERE  "IsActive" = TRUE
                                                       ORDER  BY "ConsultationTypeId"
                                                       """;
}
