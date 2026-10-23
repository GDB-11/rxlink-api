using Application.Core.DTOs.PatientAccess;
using Application.Core.DTOs.PatientAccess.Errors;
using Application.Core.Services.PatientAccess;
using BindSharp;
using Infrastructure.Core.DTOs.PatientAccess;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.PatientAccess;
using Infrastructure.Core.Models.PatientAccess;
using NSubstitute;

namespace Application.Core.Services.Tests.PatientAccess;

public sealed class PatientAccessPolicyTests
{
    private readonly IPatientAccessRepository _repository = Substitute.For<IPatientAccessRepository>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly PatientAccessPolicy _sut;
    private readonly Guid _userCode = Guid.NewGuid();
    private readonly Guid _patientCode = Guid.NewGuid();

    public PatientAccessPolicyTests() => _sut = new PatientAccessPolicy(_repository, _userContext);

    private void GivenCaller(string? role, Guid? userCode = null)
    {
        _userContext.Role.Returns(role);
        _userContext.UserCode.Returns(userCode ?? _userCode);
        // Real patient tokens carry the patient code in its own claim and no user code.
        if (role == "Patient")
        {
            _userContext.UserCode.Returns((Guid?)null);
            _userContext.PatientCode.Returns(userCode ?? _userCode);
        }
    }

    private void GivenFacts(bool canViewClinical = true, bool inCareTeam = false, bool hasBreakGlass = false) =>
        _repository.GetFactsAsync(_userCode, Arg.Any<Guid>())
            .Returns(Task.FromResult(Result<PatientAccessFactsRow, PatientAccessRepositoryError>.Success(
                new PatientAccessFactsRow
                {
                    CanViewClinicalData = canViewClinical,
                    InCareTeam = inCareTeam,
                    HasBreakGlass = hasBreakGlass
                })));

    // ── Patient ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Patient_OwnRecord_IsAllowedWithoutQuery()
    {
        GivenCaller("Patient", _patientCode);

        var result = await _sut.EvaluateAsync(_patientCode, PatientDataScope.Clinical);

        Assert.Equal(PatientAccessDecision.Allowed, result.Value);
        await _repository.DidNotReceiveWithAnyArgs().GetFactsAsync(default, default);
    }

    [Fact]
    public async Task Patient_OtherRecord_IsDenied()
    {
        GivenCaller("Patient", Guid.NewGuid());

        var result = await _sut.EvaluateAsync(_patientCode, PatientDataScope.Administrative);

        Assert.Equal(PatientAccessDecision.Denied, result.Value);
    }

    // ── Doctor ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Doctor_AnyPatient_IsAllowed()
    {
        GivenCaller("Doctor");
        GivenFacts(inCareTeam: false);

        var result = await _sut.EvaluateAsync(_patientCode, PatientDataScope.Clinical);

        Assert.Equal(PatientAccessDecision.Allowed, result.Value);
    }

    // ── Nurse ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Nurse_InCareTeam_IsAllowed()
    {
        GivenCaller("Enfermero");
        GivenFacts(inCareTeam: true);

        var result = await _sut.EvaluateAsync(_patientCode, PatientDataScope.Clinical);

        Assert.Equal(PatientAccessDecision.Allowed, result.Value);
    }

    [Fact]
    public async Task Nurse_OutsideCareTeamWithoutGrant_IsDenied()
    {
        GivenCaller("Enfermero");
        GivenFacts(inCareTeam: false, hasBreakGlass: false);

        var result = await _sut.EvaluateAsync(_patientCode, PatientDataScope.Clinical);

        Assert.Equal(PatientAccessDecision.Denied, result.Value);
    }

    [Fact]
    public async Task Nurse_OutsideCareTeamWithGrant_IsAllowedViaBreakGlass()
    {
        GivenCaller("Enfermero");
        GivenFacts(inCareTeam: false, hasBreakGlass: true);

        var result = await _sut.EvaluateAsync(_patientCode, PatientDataScope.Clinical);

        Assert.Equal(PatientAccessDecision.AllowedViaBreakGlass, result.Value);
    }

    [Fact]
    public async Task Nurse_AdministrativeScope_AlsoNeedsCareTeamOrGrant()
    {
        GivenCaller("Enfermero");
        GivenFacts(inCareTeam: false);

        var result = await _sut.EvaluateAsync(_patientCode, PatientDataScope.Administrative);

        Assert.Equal(PatientAccessDecision.Denied, result.Value);
    }

    // ── Administrator ───────────────────────────────────────────────────────

    [Fact]
    public async Task Admin_AdministrativeScope_IsAllowedWithoutQuery()
    {
        GivenCaller("Administrador");

        var result = await _sut.EvaluateAsync(_patientCode, PatientDataScope.Administrative);

        Assert.Equal(PatientAccessDecision.Allowed, result.Value);
        await _repository.DidNotReceiveWithAnyArgs().GetFactsAsync(default, default);
    }

    [Fact]
    public async Task Admin_ClinicalScopeWithoutPermission_IsDenied()
    {
        GivenCaller("Administrador");
        GivenFacts(canViewClinical: false);

        var result = await _sut.EvaluateAsync(_patientCode, PatientDataScope.Clinical);

        Assert.Equal(PatientAccessDecision.Denied, result.Value);
    }

    [Fact]
    public async Task Admin_ClinicalScopeWithPermission_IsAllowed()
    {
        GivenCaller("Administrador");
        GivenFacts(canViewClinical: true);

        var result = await _sut.EvaluateAsync(_patientCode, PatientDataScope.Clinical);

        Assert.Equal(PatientAccessDecision.Allowed, result.Value);
    }

    // ── Unknown / anonymous ─────────────────────────────────────────────────

    [Fact]
    public async Task UnknownRole_IsDenied()
    {
        GivenCaller("Recepcionista");

        var result = await _sut.EvaluateAsync(_patientCode, PatientDataScope.Administrative);

        Assert.Equal(PatientAccessDecision.Denied, result.Value);
    }

    [Fact]
    public async Task Anonymous_IsDenied()
    {
        _userContext.Role.Returns((string?)null);
        _userContext.UserCode.Returns((Guid?)null);

        var result = await _sut.EvaluateAsync(_patientCode, PatientDataScope.Administrative);

        Assert.Equal(PatientAccessDecision.Denied, result.Value);
    }

    [Fact]
    public async Task FactsFailure_IsPropagatedAsDataAccessError()
    {
        GivenCaller("Enfermero");
        _repository.GetFactsAsync(_userCode, _patientCode)
            .Returns(Task.FromResult(Result<PatientAccessFactsRow, PatientAccessRepositoryError>.Failure(
                new GetPatientAccessFactsError("boom"))));

        var result = await _sut.EvaluateAsync(_patientCode, PatientDataScope.Clinical);

        Assert.True(result.IsFailure);
        Assert.IsType<PatientAccessDataAccessError>(result.Error);
    }

    [Fact]
    public async Task RepeatedChecksForSamePatient_QueryOnce()
    {
        GivenCaller("Enfermero");
        GivenFacts(inCareTeam: true);

        await _sut.EvaluateAsync(_patientCode, PatientDataScope.Administrative);
        await _sut.EvaluateAsync(_patientCode, PatientDataScope.Clinical);

        await _repository.Received(1).GetFactsAsync(_userCode, _patientCode);
    }

    // ── List scope ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListScope_Nurse_IsRestrictedToOwnCareTeam()
    {
        GivenCaller("Enfermero");
        GivenFacts(canViewClinical: true);

        var result = await _sut.GetListScopeAsync();

        Assert.Equal(_userCode, result.Value.RestrictToCareTeamOfUserCode);
        Assert.True(result.Value.IncludeClinical);
    }

    [Fact]
    public async Task ListScope_AdminWithoutPermission_IsUnrestrictedButWithoutClinicalData()
    {
        GivenCaller("Administrador");
        GivenFacts(canViewClinical: false);

        var result = await _sut.GetListScopeAsync();

        Assert.Null(result.Value.RestrictToCareTeamOfUserCode);
        Assert.False(result.Value.IncludeClinical);
    }

    [Fact]
    public async Task ListScope_UnknownRole_SeesNobody()
    {
        GivenCaller("Recepcionista");

        var result = await _sut.GetListScopeAsync();

        Assert.Equal(Guid.Empty, result.Value.RestrictToCareTeamOfUserCode);
        Assert.False(result.Value.IncludeClinical);
    }
}
