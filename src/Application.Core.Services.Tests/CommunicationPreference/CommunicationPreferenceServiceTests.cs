using Application.Core.DTOs.CommunicationPreference.Errors;
using Application.Core.DTOs.CommunicationPreference.Request;
using Application.Core.DTOs.CommunicationPreference.Response;
using Application.Core.DTOs.PatientAccess;
using Application.Core.DTOs.PatientAccess.Errors;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Services.CommunicationPreference;
using BindSharp;
using Infrastructure.Core.DTOs.CommunicationPreference;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.CommunicationPreference;
using Infrastructure.Core.Models.CommunicationPreference;
using NSubstitute;

namespace Application.Core.Services.Tests.CommunicationPreference;

public sealed class CommunicationPreferenceServiceTests
{
    private readonly ICommunicationPreferenceRepository _repository = Substitute.For<ICommunicationPreferenceRepository>();
    private readonly IPatientAccessPolicy _policy = Substitute.For<IPatientAccessPolicy>();
    private readonly IRecordAccessLogger _accessLogger = Substitute.For<IRecordAccessLogger>();
    private readonly IActivityLogger _activity = Substitute.For<IActivityLogger>();
    private readonly CommunicationPreferenceService _sut;
    private readonly Guid _patientCode = Guid.NewGuid();
    private readonly Guid _user = Guid.NewGuid();

    public CommunicationPreferenceServiceTests()
    {
        _sut = new CommunicationPreferenceService(_repository, _policy, _accessLogger, _activity);
        _policy.EvaluateAsync(Arg.Any<Guid>(), Arg.Any<PatientDataScope>())
            .Returns(Result<PatientAccessDecision, PatientAccessError>.Success(PatientAccessDecision.Allowed));
        _repository.UpsertAsync(default, default, default, default, default).ReturnsForAnyArgs(
            Result<CommunicationPreferenceRow?, CommunicationPreferenceRepositoryError>.Success(Row()));
    }

    [Theory]
    [InlineData("sin-arroba", null)]
    [InlineData(null, "abc")]
    [InlineData(null, "12")]
    public async Task Update_rejects_an_invalid_alternative_contact(string? email, string? phone)
    {
        Result<CommunicationPreferenceResponse, CommunicationPreferenceError> result =
            await _sut.UpdateAsync(_patientCode, Request(email, phone), _user);

        Assert.IsType<CommunicationPreferenceInvalidError>(result.Error);
        await _repository.DidNotReceiveWithAnyArgs().UpsertAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task Update_trims_and_turns_blank_contacts_into_null()
    {
        await _sut.UpdateAsync(_patientCode, Request("  a@b.pe ", "   "), _user);

        await _repository.Received(1).UpsertAsync(_patientCode, false, true, "a@b.pe", null);
    }

    [Fact]
    public async Task Update_logs_flags_but_never_the_alternative_contact()
    {
        await _sut.UpdateAsync(_patientCode, Request("secreto@correo.pe", "+51 999 888 777"), _user);

        await _activity.Received(1).LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.CommunicationPreferencesUpdated
            && e.ActorUserCode == _user
            && !e.Payload!.ToString()!.Contains("secreto")
            && !e.Payload!.ToString()!.Contains("999")));
    }

    [Fact]
    public async Task Update_own_has_no_staff_actor_and_skips_the_access_policy()
    {
        Result<CommunicationPreferenceResponse, CommunicationPreferenceError> result =
            await _sut.UpdateOwnAsync(_patientCode, Request(null, null));

        Assert.True(result.IsSuccess);
        await _policy.DidNotReceiveWithAnyArgs().EvaluateAsync(default, default);
        await _activity.Received(1).LogAsync(Arg.Is<ActivityEvent>(e => e.ActorUserCode == null));
    }

    [Fact]
    public async Task Update_is_denied_when_the_policy_denies_the_patient()
    {
        _policy.EvaluateAsync(_patientCode, PatientDataScope.Administrative)
            .Returns(Result<PatientAccessDecision, PatientAccessError>.Success(PatientAccessDecision.Denied));

        Result<CommunicationPreferenceResponse, CommunicationPreferenceError> result =
            await _sut.UpdateAsync(_patientCode, Request(null, null), _user);

        Assert.IsType<CommunicationPreferenceAccessDeniedError>(result.Error);
    }

    [Fact]
    public async Task Update_reports_a_missing_patient()
    {
        _repository.UpsertAsync(default, default, default, default, default).ReturnsForAnyArgs(
            Result<CommunicationPreferenceRow?, CommunicationPreferenceRepositoryError>.Success(null));

        Result<CommunicationPreferenceResponse, CommunicationPreferenceError> result =
            await _sut.UpdateAsync(_patientCode, Request(null, null), _user);

        Assert.IsType<CommunicationPreferencePatientNotFoundError>(result.Error);
    }

    [Fact]
    public async Task Staff_read_is_written_to_the_access_log()
    {
        _repository.GetAsync(_patientCode).Returns(
            Result<CommunicationPreferenceRow?, CommunicationPreferenceRepositoryError>.Success(Row()));

        Result<CommunicationPreferenceResponse, CommunicationPreferenceError> result = await _sut.GetAsync(_patientCode);

        Assert.True(result.IsSuccess);
        await _accessLogger.Received(1).LogAsync(AccessResourceTypes.CommunicationPreferences, _patientCode, null, false);
    }

    private static UpdateCommunicationPreferenceRequest Request(string? email, string? phone) =>
        new() { AllowEmail = false, AllowSms = true, AlternativeEmail = email, AlternativePhone = phone };

    private CommunicationPreferenceRow Row() =>
        new() { PatientCode = _patientCode, AllowEmail = false, AllowSms = true, UpdatedAt = DateTimeOffset.UtcNow };
}
