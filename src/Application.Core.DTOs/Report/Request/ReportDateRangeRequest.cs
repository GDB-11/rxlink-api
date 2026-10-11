namespace Application.Core.DTOs.Report.Request;

/// <summary>Mandatory inclusive date range (UTC days) for every report.</summary>
public sealed record ReportDateRangeRequest
{
    public required DateOnly From { get; init; }
    public required DateOnly To { get; init; }
}
