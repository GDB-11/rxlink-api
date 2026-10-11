namespace Infrastructure.Core.Services.PatientAccess;

/// <summary>
/// SQL fragments shared by every query that must agree on who is "in the care team".
/// The enclosing query must alias the patient table as <c>pat</c>.
/// </summary>
internal static class CareTeamSql
{
    /// <summary>
    /// True when the patient has an <c>Activo</c> prescription that is still valid, or an active
    /// appointment today (Lima time).
    /// </summary>
    internal const string Predicate = """
                                      (EXISTS (SELECT 1
                                               FROM "Prescription" pr
                                               JOIN "PrescriptionStatus" ps ON ps."PrescriptionStatusId" = pr."PrescriptionStatusId"
                                               WHERE pr."PatientId" = pat."PatientId"
                                                 AND ps."Name" = 'Activo'
                                                 AND pr."DeletedAt" IS NULL
                                                 AND pr."ValidUntil" >= (NOW() AT TIME ZONE 'America/Lima')::date)
                                       OR EXISTS (SELECT 1
                                                  FROM "Appointment" ap
                                                  JOIN "AppointmentStatus" st ON st."AppointmentStatusId" = ap."AppointmentStatusId"
                                                  WHERE ap."PatientId" = pat."PatientId"
                                                    AND st."Name" IN ('Confirmado', 'Completado')
                                                    AND (ap."ScheduledAt" AT TIME ZONE 'America/Lima')::date
                                                        = (NOW() AT TIME ZONE 'America/Lima')::date))
                                      """;

    /// <summary>True when the user <c>@UserCode</c> holds an unexpired break-glass grant for the patient.</summary>
    internal const string ActiveBreakGlassPredicate = """
                                                      EXISTS (SELECT 1
                                                              FROM "BreakGlassGrant" bg
                                                              JOIN "User" bgu ON bgu."UserId" = bg."UserId"
                                                              WHERE bg."PatientId" = pat."PatientId"
                                                                AND bgu."UserCode" = @UserCode
                                                                AND bg."ExpiresAt" > NOW())
                                                      """;
}
