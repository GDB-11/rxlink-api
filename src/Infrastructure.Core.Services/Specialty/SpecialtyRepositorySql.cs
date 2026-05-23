namespace Infrastructure.Core.Services.Specialty;

internal static class SpecialtyRepositorySql
{
    internal const string GetPage = """
        SELECT 
            SpecialtyCode,
            Name,
            IsActive,
            COUNT(*) OVER() AS TotalCount
        FROM Specialties
        WHERE (@Search IS NULL OR Name ILIKE '%' || @Search || '%')
        ORDER BY Name
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
    """;
    /// <summary>
    /// Inserts a new spoecialty and returns the full row (including joined names) via CTE.
    /// </summary>
    internal const string Insert = """
        WITH Inserted AS (
            INSERT INTO Specialties (Name)
            VALUES (@Name)
            RETURNING SpecialtyCode
        )
        SELECT 
            s.SpecialtyCode,
            s.Name,
            s.IsActive,
            0 AS TotalCount
        FROM Specialties s
        JOIN Inserted i ON s.SpecialtyCode = i.SpecialtyCode;
    """;
    
    /// <summary>
    /// Updates an active specialty by code and returns the updated full row via CTE.
    /// Returns no rows when the code does not match an active record.
    /// </summary>
    internal const string Update = """
        WITH Updated AS (
            UPDATE Specialties
            SET Name = @Name
            WHERE SpecialtyCode = @Code AND IsActive = TRUE
            RETURNING SpecialtyCode
        )
        SELECT 
            s.SpecialtyCode,
            s.Name,
            s.IsActive,
            0 AS TotalCount
        FROM Specialties s
        JOIN Updated u ON s.SpecialtyCode = u.SpecialtyCode;
    """;
     /// <summary>
    /// Soft-deletes an active specialty.
    /// <c>DeletedBy</c> is resolved from the caller's <c>UserCode</c> via a subquery.
    /// Affects 0 rows when the code does not match an active record.
    /// </summary>
    internal const string Deactivate = """
        UPDATE Specialties
        SET IsActive = FALSE, DeletedBy = (SELECT UserCode FROM Users WHERE UserCode = @PerformedByUserCode)
        WHERE SpecialtyCode = @Code AND IsActive = TRUE;
    """;
    /// <summary>
    /// Reactivates a previously deactivated specialty.
    /// Affects 0 rows when the code does not match an active record.
    /// </summary>
    internal const string Activate = """
        UPDATE Specialties
        SET IsActive = TRUE, DeletedBy = NULL
        WHERE SpecialtyCode = @Code AND IsActive = FALSE;
    """;
}

    