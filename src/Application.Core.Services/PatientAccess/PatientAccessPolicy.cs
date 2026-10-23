using Application.Core.DTOs.PatientAccess;
using Application.Core.DTOs.PatientAccess.Errors;
using Application.Core.Interfaces.PatientAccess;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.PatientAccess;
using Infrastructure.Core.Models.PatientAccess;

namespace Application.Core.Services.PatientAccess;

public sealed class PatientAccessPolicy : IPatientAccessPolicy
{
    private const string PatientRole = "Patient";
    private const string DoctorRole = "Doctor";
    private const string NurseRole = "Enfermero";
    private const string AdminRole = "Administrador";

    private readonly IPatientAccessRepository _repository;
    private readonly IUserContext _userContext;

    // The policy is scoped (one instance per request), so a request that checks the same patient several
    // times (profile + allergies, then diagnostics) hits the database once.
    private readonly Dictionary<Guid, PatientAccessFactsRow> _factsCache = new();

    public PatientAccessPolicy(IPatientAccessRepository repository, IUserContext userContext)
    {
        _repository = repository;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<Result<PatientAccessDecision, PatientAccessError>> EvaluateAsync(
        Guid patientCode, PatientDataScope scope)
    {
        Guid? userCode = _userContext.UserCode;
        string? role = _userContext.Role;

        // Patients are not User rows (no UserCode): their own record is the only one they may read.
        if (role == PatientRole)
            return Success(_userContext.PatientCode == patientCode ? PatientAccessDecision.Allowed : PatientAccessDecision.Denied);

        if (userCode is null)
            return Success(PatientAccessDecision.Denied);

        if (role is not (DoctorRole or NurseRole or AdminRole))
            return Success(PatientAccessDecision.Denied);

        // Doctors and administrators do not depend on the care team, so administrative reads need no query.
        if (role is DoctorRole or AdminRole && scope == PatientDataScope.Administrative)
            return Success(PatientAccessDecision.Allowed);

        return await GetFactsAsync(userCode.Value, patientCode)
            .MapAsync(facts => Decide(role, scope, facts));
    }

    /// <inheritdoc/>
    public async Task<Result<PatientListScope, PatientAccessError>> GetListScopeAsync()
    {
        Guid? userCode = _userContext.UserCode;
        string? role = _userContext.Role;

        if (userCode is null || role is not (DoctorRole or NurseRole or AdminRole))
            return Result<PatientListScope, PatientAccessError>.Success(new PatientListScope(Guid.Empty, false));

        return await GetFactsAsync(userCode.Value, Guid.Empty)
            .MapAsync(facts => new PatientListScope(
                role == NurseRole ? userCode : null,
                facts.CanViewClinicalData));
    }

    private static PatientAccessDecision Decide(string role, PatientDataScope scope, PatientAccessFactsRow facts)
    {
        if (scope == PatientDataScope.Clinical && !facts.CanViewClinicalData)
            return PatientAccessDecision.Denied;

        if (role != NurseRole)
            return PatientAccessDecision.Allowed;

        if (facts.InCareTeam)
            return PatientAccessDecision.Allowed;

        return facts.HasBreakGlass ? PatientAccessDecision.AllowedViaBreakGlass : PatientAccessDecision.Denied;
    }

    private async Task<Result<PatientAccessFactsRow, PatientAccessError>> GetFactsAsync(
        Guid userCode, Guid patientCode)
    {
        if (_factsCache.TryGetValue(patientCode, out PatientAccessFactsRow? cached))
            return Result<PatientAccessFactsRow, PatientAccessError>.Success(cached);

        return await _repository.GetFactsAsync(userCode, patientCode)
            .MapErrorAsync(PatientAccessError (error) =>
                new PatientAccessDataAccessError(error.Message, error.Details, error.Exception))
            .TapAsync(facts => _factsCache[patientCode] = facts);
    }

    private static Result<PatientAccessDecision, PatientAccessError> Success(PatientAccessDecision decision) =>
        Result<PatientAccessDecision, PatientAccessError>.Success(decision);
}
