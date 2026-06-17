namespace Infrastructure.Core.Services.Appointment;

internal static class AppointmentRepositorySql
{
    internal const string GetPatientId = """
                                         SELECT "PatientId"
                                         FROM "Patient"
                                         WHERE "PatientCode" = @PatientCode
                                           AND "IsActive"    = TRUE
                                           AND "DeletedAt"   IS NULL
                                         """;

    internal const string GetAvailabilitySlot = """
                                                SELECT
                                                    da."DoctorAvailabilityId",
                                                    da."DoctorId",
                                                    da."Date",
                                                    da."StartTime",
                                                    da."IsBooked",
                                                    da."DeletedAt"
                                                FROM "DoctorAvailability" da
                                                WHERE da."DoctorAvailabilityCode" = @AvailabilityCode
                                                """;

    internal const string GetConsultationTypeId = """
                                                  SELECT "ConsultationTypeId"
                                                  FROM "ConsultationType"
                                                  WHERE "ConsultationTypeCode" = @ConsultationTypeCode
                                                    AND "IsActive" = TRUE
                                                  """;

    /// <summary>
    /// Atomically marks the slot as booked.
    /// Returns 0 rows if the slot is already booked or deleted (race condition guard).
    /// </summary>
    internal const string LockSlot = """
                                     UPDATE "DoctorAvailability"
                                     SET "IsBooked" = TRUE
                                     WHERE "DoctorAvailabilityId" = @DoctorAvailabilityId
                                       AND "IsBooked"  = FALSE
                                       AND "DeletedAt" IS NULL
                                     """;

    /// <summary>Inserts the appointment and returns its generated AppointmentCode.</summary>
    internal const string InsertAppointment = """
                                              INSERT INTO "Appointment" (
                                                  "PatientId",
                                                  "DoctorId",
                                                  "DoctorAvailabilityId",
                                                  "ConsultationTypeId",
                                                  "AppointmentStatusId",
                                                  "ScheduledAt"
                                              )
                                              SELECT
                                                  @PatientId,
                                                  @DoctorId,
                                                  @DoctorAvailabilityId,
                                                  @ConsultationTypeId,
                                                  ast."AppointmentStatusId",
                                                  @ScheduledAt
                                              FROM "AppointmentStatus" ast
                                              WHERE ast."Name" = 'PendientePago'
                                              RETURNING "AppointmentCode"
                                              """;

    internal const string GetByCode = """
                                      SELECT
                                          a."AppointmentCode",
                                          pat."PatientCode",
                                          pat_per."Names"                     AS "PatientNames",
                                          pat_per."Surnames"                  AS "PatientSurnames",
                                          u."UserCode"                        AS "DoctorCode",
                                          per."Names"                         AS "DoctorNames",
                                          per."Surnames"                      AS "DoctorSurnames",
                                          sp."Name"                           AS "SpecialtyName",
                                          ct."Name"                           AS "ConsultationTypeName",
                                          ast."Name"                          AS "StatusName",
                                          ast."AppointmentStatusCode"         AS "StatusCode",
                                          a."ScheduledAt",
                                          a."CreatedAt",
                                          1                                   AS "TotalCount"
                                      FROM "Appointment" a
                                      JOIN "Patient"          pat     ON pat."PatientId"          = a."PatientId"
                                      JOIN "Person"           pat_per ON pat_per."PersonId"       = pat."PersonId"
                                      JOIN "User"             u       ON u."UserId"               = a."DoctorId"
                                      JOIN "Person"           per     ON per."PersonId"           = u."PersonId"
                                      JOIN "Specialty"        sp      ON sp."SpecialtyId"         = u."SpecialtyId"
                                      JOIN "ConsultationType" ct      ON ct."ConsultationTypeId"  = a."ConsultationTypeId"
                                      JOIN "AppointmentStatus" ast    ON ast."AppointmentStatusId" = a."AppointmentStatusId"
                                      WHERE a."AppointmentCode" = @Code
                                      """;

    internal const string GetOwnerPatientCode = """
                                                SELECT pat."PatientCode"
                                                FROM "Appointment" a
                                                JOIN "Patient" pat ON pat."PatientId" = a."PatientId"
                                                WHERE a."AppointmentCode" = @Code
                                                """;

    internal const string GetAssignedDoctorCode = """
                                                  SELECT u."UserCode"
                                                  FROM "Appointment" a
                                                  JOIN "User" u ON u."UserId" = a."DoctorId"
                                                  WHERE a."AppointmentCode" = @Code
                                                  """;

    /// <summary>
    /// Transitions PendientePago → Confirmado for the owning patient.
    /// </summary>
    internal const string ConfirmPayment = """
                                           UPDATE "Appointment" a
                                           SET
                                               "AppointmentStatusId" = (
                                                   SELECT "AppointmentStatusId" FROM "AppointmentStatus" WHERE "Name" = 'Confirmado'
                                               ),
                                               "UpdatedAt" = NOW()
                                           FROM "Patient" pat
                                           WHERE a."AppointmentCode" = @Code
                                             AND pat."PatientCode"   = @PatientCode
                                             AND pat."PatientId"     = a."PatientId"
                                             AND a."AppointmentStatusId" = (
                                                   SELECT "AppointmentStatusId" FROM "AppointmentStatus" WHERE "Name" = 'PendientePago'
                                             )
                                           """;

    /// <summary>
    /// Returns the AppointmentId and DoctorAvailabilityId for a cancellable appointment owned by the given patient.
    /// </summary>
    internal const string GetCancellableByPatient = """
                                                    SELECT a."AppointmentId", a."DoctorAvailabilityId"
                                                    FROM "Appointment" a
                                                    JOIN "Patient" pat ON pat."PatientId" = a."PatientId"
                                                    WHERE a."AppointmentCode" = @Code
                                                      AND pat."PatientCode"   = @PatientCode
                                                      AND a."AppointmentStatusId" IN (
                                                            SELECT "AppointmentStatusId" FROM "AppointmentStatus"
                                                            WHERE "Name" IN ('PendientePago', 'Confirmado')
                                                      )
                                                    """;

    /// <summary>
    /// Returns the AppointmentId and DoctorAvailabilityId for any cancellable appointment (admin).
    /// </summary>
    internal const string GetCancellableByAdmin = """
                                                  SELECT a."AppointmentId", a."DoctorAvailabilityId"
                                                  FROM "Appointment" a
                                                  WHERE a."AppointmentCode" = @Code
                                                    AND a."AppointmentStatusId" IN (
                                                          SELECT "AppointmentStatusId" FROM "AppointmentStatus"
                                                          WHERE "Name" IN ('PendientePago', 'Confirmado')
                                                    )
                                                  """;

    internal const string ReleaseSlot = """
                                        UPDATE "DoctorAvailability"
                                        SET "IsBooked" = FALSE
                                        WHERE "DoctorAvailabilityId" = @DoctorAvailabilityId
                                        """;

    internal const string SetStatusCancelled = """
                                               UPDATE "Appointment"
                                               SET
                                                   "AppointmentStatusId" = (
                                                       SELECT "AppointmentStatusId" FROM "AppointmentStatus" WHERE "Name" = 'Cancelado'
                                                   ),
                                                   "UpdatedAt" = NOW()
                                               WHERE "AppointmentId" = @AppointmentId
                                               """;

    internal const string Complete = """
                                     UPDATE "Appointment"
                                     SET
                                         "AppointmentStatusId" = (
                                             SELECT "AppointmentStatusId" FROM "AppointmentStatus" WHERE "Name" = 'Completado'
                                         ),
                                         "UpdatedAt"  = NOW(),
                                         "UpdatedBy"  = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode AND "IsActive" = TRUE)
                                     WHERE "AppointmentCode" = @Code
                                       AND "AppointmentStatusId" = (
                                             SELECT "AppointmentStatusId" FROM "AppointmentStatus" WHERE "Name" = 'Confirmado'
                                       )
                                     """;

    internal const string NoShow = """
                                   UPDATE "Appointment"
                                   SET
                                       "AppointmentStatusId" = (
                                           SELECT "AppointmentStatusId" FROM "AppointmentStatus" WHERE "Name" = 'NoAsistio'
                                       ),
                                       "UpdatedAt"  = NOW(),
                                       "UpdatedBy"  = (SELECT "UserId" FROM "User" WHERE "UserCode" = @PerformedByUserCode AND "IsActive" = TRUE)
                                   WHERE "AppointmentCode" = @Code
                                     AND "AppointmentStatusId" = (
                                           SELECT "AppointmentStatusId" FROM "AppointmentStatus" WHERE "Name" = 'Confirmado'
                                     )
                                   """;

    internal const string GetPatientAppointments = """
                                                   SELECT
                                                       a."AppointmentCode",
                                                       pat."PatientCode",
                                                       pat_per."Names"                     AS "PatientNames",
                                                       pat_per."Surnames"                  AS "PatientSurnames",
                                                       u."UserCode"                        AS "DoctorCode",
                                                       per."Names"                         AS "DoctorNames",
                                                       per."Surnames"                      AS "DoctorSurnames",
                                                       sp."Name"                           AS "SpecialtyName",
                                                       ct."Name"                           AS "ConsultationTypeName",
                                                       ast."Name"                          AS "StatusName",
                                                       ast."AppointmentStatusCode"         AS "StatusCode",
                                                       a."ScheduledAt",
                                                       a."CreatedAt",
                                                       COUNT(*) OVER()                     AS "TotalCount"
                                                   FROM "Appointment" a
                                                   JOIN "Patient"          pat     ON pat."PatientId"          = a."PatientId"
                                                   JOIN "Person"           pat_per ON pat_per."PersonId"       = pat."PersonId"
                                                   JOIN "User"             u       ON u."UserId"               = a."DoctorId"
                                                   JOIN "Person"           per     ON per."PersonId"           = u."PersonId"
                                                   JOIN "Specialty"        sp      ON sp."SpecialtyId"         = u."SpecialtyId"
                                                   JOIN "ConsultationType" ct      ON ct."ConsultationTypeId"  = a."ConsultationTypeId"
                                                   JOIN "AppointmentStatus" ast    ON ast."AppointmentStatusId" = a."AppointmentStatusId"
                                                   WHERE pat."PatientCode" = @PatientCode
                                                   ORDER BY a."ScheduledAt" DESC
                                                   LIMIT @PageSize OFFSET @Offset
                                                   """;

    internal const string GetDoctorAppointments = """
                                                  SELECT
                                                      a."AppointmentCode",
                                                      pat."PatientCode",
                                                      pat_per."Names"                     AS "PatientNames",
                                                      pat_per."Surnames"                  AS "PatientSurnames",
                                                      u."UserCode"                        AS "DoctorCode",
                                                      per."Names"                         AS "DoctorNames",
                                                      per."Surnames"                      AS "DoctorSurnames",
                                                      sp."Name"                           AS "SpecialtyName",
                                                      ct."Name"                           AS "ConsultationTypeName",
                                                      ast."Name"                          AS "StatusName",
                                                      ast."AppointmentStatusCode"         AS "StatusCode",
                                                      a."ScheduledAt",
                                                      a."CreatedAt",
                                                      COUNT(*) OVER()                     AS "TotalCount"
                                                  FROM "Appointment" a
                                                  JOIN "Patient"          pat     ON pat."PatientId"           = a."PatientId"
                                                  JOIN "Person"           pat_per ON pat_per."PersonId"        = pat."PersonId"
                                                  JOIN "User"             u       ON u."UserId"                = a."DoctorId"
                                                  JOIN "Person"           per     ON per."PersonId"            = u."PersonId"
                                                  JOIN "Specialty"        sp      ON sp."SpecialtyId"          = u."SpecialtyId"
                                                  JOIN "ConsultationType" ct      ON ct."ConsultationTypeId"   = a."ConsultationTypeId"
                                                  JOIN "AppointmentStatus" ast    ON ast."AppointmentStatusId" = a."AppointmentStatusId"
                                                  WHERE u."UserCode" = @DoctorCode
                                                    AND (@Date::date       IS NULL OR a."ScheduledAt"::date = @Date::date)
                                                    AND (@StatusName::text IS NULL OR ast."Name"            = @StatusName::text)
                                                  ORDER BY a."ScheduledAt" ASC
                                                  LIMIT @PageSize OFFSET @Offset
                                                  """;

    internal const string ConfirmPaymentByAdmin = """
                                                  UPDATE "Appointment"
                                                  SET    "AppointmentStatusId" = (
                                                             SELECT "AppointmentStatusId" FROM "AppointmentStatus" WHERE "Name" = 'Confirmado'
                                                         ),
                                                         "UpdatedAt" = NOW()
                                                  WHERE  "AppointmentCode" = @Code
                                                    AND  "AppointmentStatusId" = (
                                                             SELECT "AppointmentStatusId" FROM "AppointmentStatus" WHERE "Name" = 'PendientePago'
                                                         )
                                                  """;

    internal const string RevertPayment = """
                                          UPDATE "Appointment"
                                          SET    "AppointmentStatusId" = (
                                                     SELECT "AppointmentStatusId" FROM "AppointmentStatus" WHERE "Name" = 'PendientePago'
                                                 ),
                                                 "UpdatedAt" = NOW()
                                          WHERE  "AppointmentCode" = @Code
                                            AND  "AppointmentStatusId" = (
                                                     SELECT "AppointmentStatusId" FROM "AppointmentStatus" WHERE "Name" = 'Confirmado'
                                                 )
                                          """;

    internal const string GetAdminAppointments = """
                                                 SELECT
                                                     a."AppointmentCode",
                                                     pat."PatientCode",
                                                     pat_per."Names"                     AS "PatientNames",
                                                     pat_per."Surnames"                  AS "PatientSurnames",
                                                     u."UserCode"                        AS "DoctorCode",
                                                     per."Names"                         AS "DoctorNames",
                                                     per."Surnames"                      AS "DoctorSurnames",
                                                     sp."Name"                           AS "SpecialtyName",
                                                     ct."Name"                           AS "ConsultationTypeName",
                                                     ast."Name"                          AS "StatusName",
                                                     ast."AppointmentStatusCode"         AS "StatusCode",
                                                     a."ScheduledAt",
                                                     a."CreatedAt",
                                                     COUNT(*) OVER()                     AS "TotalCount"
                                                 FROM "Appointment" a
                                                 JOIN "Patient"           pat     ON pat."PatientId"           = a."PatientId"
                                                 JOIN "Person"            pat_per ON pat_per."PersonId"        = pat."PersonId"
                                                 JOIN "User"              u       ON u."UserId"                = a."DoctorId"
                                                 JOIN "Person"            per     ON per."PersonId"            = u."PersonId"
                                                 JOIN "Specialty"         sp      ON sp."SpecialtyId"          = u."SpecialtyId"
                                                 JOIN "ConsultationType"  ct      ON ct."ConsultationTypeId"   = a."ConsultationTypeId"
                                                 JOIN "AppointmentStatus" ast     ON ast."AppointmentStatusId" = a."AppointmentStatusId"
                                                 WHERE (@PatientSearch::text IS NULL
                                                        OR pat_per."Names"     ILIKE '%' || @PatientSearch || '%'
                                                        OR pat_per."Surnames"  ILIKE '%' || @PatientSearch || '%')
                                                   AND (@Date::date       IS NULL OR a."ScheduledAt"::date = @Date::date)
                                                   AND (@StatusName::text IS NULL OR ast."Name"            = @StatusName::text)
                                                 ORDER BY a."ScheduledAt" DESC
                                                 LIMIT @PageSize OFFSET @Offset
                                                 """;
}