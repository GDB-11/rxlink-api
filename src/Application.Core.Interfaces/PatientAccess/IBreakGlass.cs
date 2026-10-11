using Application.Core.DTOs.PatientAccess.Errors;
using Application.Core.DTOs.PatientAccess.Request;
using Application.Core.DTOs.PatientAccess.Response;
using Application.Core.DTOs.Report.Request;
using BindSharp;

namespace Application.Core.Interfaces.PatientAccess;

public interface IBreakGlass
{
    /// <summary>Opens a patient outside the nurse's care team for a limited time, with a mandatory reason.</summary>
    Task<Result<BreakGlassResponse, PatientAccessError>> RequestAsync(BreakGlassRequest request);

    /// <summary>Break-glass grants in a date range, for the administrator.</summary>
    Task<Result<IReadOnlyList<BreakGlassReportItemResponse>, PatientAccessError>> GetReportAsync(
        ReportDateRangeRequest request);
}
