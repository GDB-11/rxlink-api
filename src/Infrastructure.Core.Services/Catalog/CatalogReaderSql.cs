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

    public const string MedicationClasses =
        """SELECT "MedicationClassCode" AS Code, "Name", "IsActive" FROM "MedicationClass" """;

    public const string MedicationClassMembers =
        """
        SELECT c."MedicationClassCode" AS ClassCode, m."MedicationCode" AS MedicationCode
        FROM "MedicationClassMember" cm
        JOIN "MedicationClass" c ON c."MedicationClassId" = cm."MedicationClassId"
        JOIN "Medication"      m ON m."MedicationId"      = cm."MedicationId"
        WHERE cm."IsActive"
        """;

    public const string AllergyMedicationClasses =
        """
        SELECT a."AllergyCode" AS AllergyCode, c."MedicationClassCode" AS ClassCode
        FROM "AllergyMedicationClass" amc
        JOIN "Allergy"         a ON a."AllergyId"         = amc."AllergyId"
        JOIN "MedicationClass" c ON c."MedicationClassId" = amc."MedicationClassId"
        WHERE amc."IsActive"
        """;

    public const string MedicationInteractions =
        """
        SELECT i."MedicationInteractionCode" AS Code,
               ca."MedicationClassCode"      AS ClassACode,
               cb."MedicationClassCode"      AS ClassBCode,
               i."Severity",
               i."Description"
        FROM "MedicationInteraction" i
        JOIN "MedicationClass" ca ON ca."MedicationClassId" = i."MedicationClassAId"
        JOIN "MedicationClass" cb ON cb."MedicationClassId" = i."MedicationClassBId"
        WHERE i."IsActive"
        """;
}
