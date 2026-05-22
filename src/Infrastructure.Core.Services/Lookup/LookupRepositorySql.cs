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
}
