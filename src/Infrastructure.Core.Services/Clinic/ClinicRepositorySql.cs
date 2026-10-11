namespace Infrastructure.Core.Services.Clinic;

/// <summary>
/// SQL used exclusively by <see cref="ClinicRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class ClinicRepositorySql
{
    /// <summary>Returns the active default clinic (a partial unique index guarantees at most one default).</summary>
    internal const string GetDefault = """
                                       SELECT
                                           c."ClinicCode",
                                           c."Name",
                                           c."Address",
                                           c."District",
                                           c."Province",
                                           c."Department",
                                           c."Phone",
                                           c."Email",
                                           c."Website",
                                           c."FacilityRegistrationCode",
                                           c."UpdatedAt"
                                       FROM "Clinic" c
                                       WHERE c."IsDefault" = TRUE AND c."IsActive" = TRUE
                                       """;

    /// <summary>
    /// Updates the operational fields of the active default clinic and returns the updated row via CTE.
    /// <c>UpdatedBy</c> is resolved from the caller's user code; it stays NULL for a missing caller.
    /// </summary>
    internal const string UpdateDefault = """
                                          WITH upd AS (
                                              UPDATE "Clinic"
                                              SET "Address"                  = @Address,
                                                  "District"                 = @District,
                                                  "Province"                 = @Province,
                                                  "Department"               = @Department,
                                                  "Phone"                    = @Phone,
                                                  "Email"                    = @Email,
                                                  "Website"                  = @Website,
                                                  "FacilityRegistrationCode" = @FacilityRegistrationCode,
                                                  "UpdatedAt"                = NOW(),
                                                  "UpdatedBy"                = (SELECT "UserId" FROM "User" WHERE "UserCode" = @UserCode)
                                              WHERE "IsDefault" = TRUE AND "IsActive" = TRUE
                                              RETURNING *
                                          )
                                          SELECT
                                              upd."ClinicCode",
                                              upd."Name",
                                              upd."Address",
                                              upd."District",
                                              upd."Province",
                                              upd."Department",
                                              upd."Phone",
                                              upd."Email",
                                              upd."Website",
                                              upd."FacilityRegistrationCode",
                                              upd."UpdatedAt"
                                          FROM upd
                                          """;
}
