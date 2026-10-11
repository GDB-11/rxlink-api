using Application.Core.DTOs.PatientAccess;
using Application.Core.DTOs.PatientAccess.Errors;
using Application.Core.Interfaces.PatientAccess;
using BindSharp;
using BindSharp.Extensions;

namespace Application.Core.Services.PatientAccess;

internal static class PatientAccessExtensions
{
    /// <summary>
    /// Runs the policy and turns anything but an allow into the calling service's own error:
    /// <paramref name="forbidden"/> for <see cref="PatientAccessDecision.Denied"/>, <paramref name="dataError"/> for a failed lookup.
    /// On success the decision tells the caller whether the read must be logged as break-glass.
    /// </summary>
    internal static Task<Result<PatientAccessDecision, TError>> RequireAsync<TError>(
        this IPatientAccessPolicy policy,
        Guid patientCode,
        PatientDataScope scope,
        Func<PatientAccessDataAccessError, TError> dataError,
        TError forbidden) =>
        policy.EvaluateAsync(patientCode, scope)
            .MapErrorAsync(error => dataError(error as PatientAccessDataAccessError
                                              ?? new PatientAccessDataAccessError(error.Message)))
            .EnsureAsync(decision => decision != PatientAccessDecision.Denied, forbidden);
}
