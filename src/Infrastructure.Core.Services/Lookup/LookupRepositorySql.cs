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
}
