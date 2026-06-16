namespace Infrastructure.Core.Services.Availability;

/// <summary>
/// SQL used exclusively by <see cref="AvailabilityRepository"/>.
/// All identifiers are double-quoted to honour the PascalCase DDL convention.
/// </summary>
internal static class AvailabilityRepositorySql
{
    /// <summary>
    /// Returns the UserId for an active Doctor identified by UserCode.
    /// Returns no rows when the code does not match an active Doctor.
    /// </summary>
    internal const string GetDoctorUserId = """
                                            SELECT u."UserId"
                                            FROM "User" u
                                            JOIN "Role" r ON r."RoleId" = u."RoleId"
                                            WHERE u."UserCode"  = @DoctorCode
                                              AND u."IsActive"  = TRUE
                                              AND u."DeletedAt" IS NULL
                                              AND r."Name"      = 'Doctor'
                                            """;

    /// <summary>
    /// Inserts one availability slot.
    /// Silently ignores duplicates (uq_doctor_date_time), returning no rows on conflict.
    /// </summary>
    internal const string InsertOne = """
                                      WITH ins AS (
                                          INSERT INTO "DoctorAvailability" ("DoctorId", "Date", "StartTime", "CreatedBy")
                                          VALUES (
                                              @DoctorUserId,
                                              @Date,
                                              @StartTime,
                                              (SELECT "UserId" FROM "User" WHERE "UserCode" = @CreatedByUserCode AND "IsActive" = TRUE)
                                          )
                                          ON CONFLICT ON CONSTRAINT uq_doctor_date_time DO NOTHING
                                          RETURNING *
                                      )
                                      SELECT
                                          ins."DoctorAvailabilityCode",
                                          ins."Date",
                                          ins."StartTime",
                                          ins."IsBooked"
                                      FROM ins
                                      """;

    /// <summary>
    /// Returns all non-deleted slots for a doctor in the given date range [StartDate, EndDate).
    /// Includes both free and booked slots so the Admin sees the full picture.
    /// </summary>
    internal const string GetByDoctorAndMonth = """
                                                SELECT
                                                    da."DoctorAvailabilityCode",
                                                    da."Date",
                                                    da."StartTime",
                                                    da."IsBooked"
                                                FROM "DoctorAvailability" da
                                                JOIN "User" u ON u."UserId" = da."DoctorId"
                                                WHERE u."UserCode"  = @DoctorCode
                                                  AND u."IsActive"  = TRUE
                                                  AND da."Date"     >= @StartDate
                                                  AND da."Date"     < @EndDate
                                                  AND da."DeletedAt" IS NULL
                                                ORDER BY da."Date", da."StartTime"
                                                """;

    /// <summary>
    /// Returns IsBooked, Date and StartTime for the slot, or no rows if not found / already soft-deleted.
    /// </summary>
    internal const string GetSlotForDeletion = """
                                               SELECT da."IsBooked", da."Date", da."StartTime"
                                               FROM "DoctorAvailability" da
                                               WHERE da."DoctorAvailabilityCode" = @Code
                                                 AND da."DeletedAt" IS NULL
                                               """;

    /// <summary>
    /// Soft-deletes a non-booked slot.
    /// The WHERE clause guards against concurrent booking or double-delete.
    /// </summary>
    internal const string SoftDelete = """
                                       UPDATE "DoctorAvailability"
                                       SET
                                           "DeletedAt" = NOW(),
                                           "DeletedBy" = (SELECT "UserId" FROM "User" WHERE "UserCode" = @DeletedByUserCode AND "IsActive" = TRUE)
                                       WHERE "DoctorAvailabilityCode" = @Code
                                         AND "DeletedAt"  IS NULL
                                         AND "IsBooked"   = FALSE
                                       """;

    /// <summary>
    /// Returns distinct dates with at least one free slot for the doctor,
    /// from today through today + 30 calendar days.
    /// </summary>
    internal const string GetAvailableDates = """
                                              SELECT DISTINCT da."Date"
                                              FROM "DoctorAvailability" da
                                              JOIN "User" u ON u."UserId" = da."DoctorId"
                                              WHERE u."UserCode"  = @DoctorCode
                                                AND u."IsActive"  = TRUE
                                                AND da."IsBooked"  = FALSE
                                                AND da."DeletedAt" IS NULL
                                                AND da."Date"      >= CURRENT_DATE
                                                AND da."Date"      <= CURRENT_DATE + INTERVAL '30 days'
                                              ORDER BY da."Date"
                                              """;

    /// <summary>
    /// Returns free slots for a doctor on a specific date, ordered by start time.
    /// </summary>
    internal const string GetAvailableSlots = """
                                              SELECT
                                                  da."DoctorAvailabilityCode",
                                                  da."StartTime"
                                              FROM "DoctorAvailability" da
                                              JOIN "User" u ON u."UserId" = da."DoctorId"
                                              WHERE u."UserCode"  = @DoctorCode
                                                AND u."IsActive"  = TRUE
                                                AND da."Date"      = @Date
                                                AND da."IsBooked"  = FALSE
                                                AND da."DeletedAt" IS NULL
                                              ORDER BY da."StartTime"
                                              """;
}