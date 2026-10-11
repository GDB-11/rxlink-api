using Application.Core.DTOs.PatientAccess;
using Application.Core.DTOs.PatientAccess.Errors;
using BindSharp;

namespace Application.Core.Interfaces.PatientAccess;

/// <summary>
/// Decides whether the current caller (taken from <c>IUserContext</c>) may read a patient's data
/// (architecture doc, Phase 2b.2). Every service that reads patient data calls it before reading.
/// <list type="bullet">
/// <item><c>Patient</c>: only their own record.</item>
/// <item><c>Doctor</c>: any patient of the partner.</item>
/// <item><c>Enfermero</c>: care team only (<c>Activo</c> prescription or appointment today); otherwise a live break-glass grant, else denied.</item>
/// <item>Clinical scope additionally needs <c>Role.CanViewClinicalData</c> (off for <c>Administrador</c> by default).</item>
/// </list>
/// </summary>
public interface IPatientAccessPolicy
{
    Task<Result<PatientAccessDecision, PatientAccessError>> EvaluateAsync(
        Guid patientCode, PatientDataScope scope);

    /// <summary>How a patient list must be narrowed (SQL predicate) and redacted for the caller.</summary>
    Task<Result<PatientListScope, PatientAccessError>> GetListScopeAsync();
}
