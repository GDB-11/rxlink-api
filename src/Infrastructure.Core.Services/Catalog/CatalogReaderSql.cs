namespace Infrastructure.Core.Services.Catalog;

public static class CatalogReaderSql
{
    public const string Version =
        """SELECT "Version" AS Version, "SchemaVersion" AS SchemaVersion FROM "CatalogVersion" """;

    public const string DocumentTypes =
        """SELECT "DocumentTypeCode" AS Code, "Name", "IsActive" FROM "DocumentType" """;

    public const string Sexes =
        """SELECT "SexCode" AS Code, "Name", "IsActive" FROM "Sex" """;

    public const string Specialties =
        """SELECT "SpecialtyCode" AS Code, "Name", "IsActive" FROM "Specialty" """;

    public const string PharmaceuticalForms =
        """SELECT "PharmaceuticalFormCode" AS Code, "Name", "IsActive" FROM "PharmaceuticalForm" """;

    public const string AdministrationRoutes =
        """SELECT "AdministrationRouteCode" AS Code, "Name", "IsActive" FROM "AdministrationRoute" """;

    public const string Frequencies =
        """SELECT "FrequencyCode" AS Code, "Description", "IntervalHours", "IsActive" FROM "Frequency" """;

    public const string Allergies =
        """SELECT "AllergyCode" AS Code, "Name", "Description", "IsActive" FROM "Allergy" """;

    public const string AllergySeverities =
        """SELECT "SeverityCode" AS Code, "Name", "SortOrder", "IsActive" FROM "AllergySeverity" """;

    public const string Medications =
        """
        SELECT m."MedicationCode"          AS Code,
               pf."PharmaceuticalFormCode" AS PharmaceuticalFormCode,
               ar."AdministrationRouteCode" AS AdministrationRouteCode,
               m."GenericName",
               m."CommercialName",
               m."Concentration",
               m."IsActive",
               m."DeletedAt"
        FROM "Medication" m
        JOIN "PharmaceuticalForm"  pf ON pf."PharmaceuticalFormId"   = m."PharmaceuticalFormId"
        JOIN "AdministrationRoute" ar ON ar."AdministrationRouteId" = m."AdministrationRouteId"
        """;

    public const string Insurances =
        """SELECT "InsuranceCode" AS Code, "Name", "DefaultCoveragePercentage", "IsActive" FROM "Insurance" """;
}
