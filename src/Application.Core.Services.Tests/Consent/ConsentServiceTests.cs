using Application.Core.DTOs.Consent.Errors;
using Application.Core.DTOs.Consent.Request;
using Application.Core.DTOs.Consent.Response;
using Application.Core.DTOs.PatientAccess;
using Application.Core.DTOs.PatientAccess.Errors;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Services.Consent;
using BindSharp;
using Infrastructure.Core.DTOs.Consent;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Consent;
using Infrastructure.Core.Models.Consent;
using NSubstitute;

namespace Application.Core.Services.Tests.Consent;

public sealed class ConsentServiceTests
{
    private readonly IConsentRepository _repository = Substitute.For<IConsentRepository>();
    private readonly IPatientAccessPolicy _policy = Substitute.For<IPatientAccessPolicy>();
    private readonly IRecordAccessLogger _accessLogger = Substitute.For<IRecordAccessLogger>();
    private readonly IActivityLogger _activity = Substitute.For<IActivityLogger>();
    private readonly ConsentService _sut;
    private readonly Guid _patientCode = Guid.NewGuid();
    private readonly Guid _user = Guid.NewGuid();

    public ConsentServiceTests()
    {
        _sut = new ConsentService(_repository, _policy, _accessLogger, _activity);
        _policy.EvaluateAsync(Arg.Any<Guid>(), Arg.Any<PatientDataScope>())
            .Returns(Result<PatientAccessDecision, PatientAccessError>.Success(PatientAccessDecision.Allowed));
    }

    [Fact]
    public async Task Record_rejects_an_unknown_type_and_a_future_date()
    {
        Result<ConsentResponse, ConsentError> unknown =
            await _sut.RecordAsync(_patientCode, Request("Vecino", DateTimeOffset.UtcNow), _user);
        Result<ConsentResponse, ConsentError> future =
            await _sut.RecordAsync(_patientCode, Request("Research", DateTimeOffset.UtcNow.AddDays(1)), _user);

        Assert.IsType<ConsentInvalidError>(unknown.Error);
        Assert.IsType<ConsentInvalidError>(future.Error);
    }

    [Fact]
    public async Task Record_logs_the_type_but_never_the_description()
    {
        _repository.InsertAsync(default, default!, default!, default, default).ReturnsForAnyArgs(
            Result<ConsentRow?, ConsentRepositoryError>.Success(Row()));

        Result<ConsentResponse, ConsentError> result =
            await _sut.RecordAsync(_patientCode, Request("Research", DateTimeOffset.UtcNow), _user);

        Assert.True(result.IsSuccess);
        await _activity.Received(1).LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.ConsentRecorded && !e.Payload!.ToString()!.Contains("estudio")));
    }

    [Fact]
    public async Task Record_is_denied_when_the_policy_denies_the_patient()
    {
        _policy.EvaluateAsync(_patientCode, PatientDataScope.Administrative)
            .Returns(Result<PatientAccessDecision, PatientAccessError>.Success(PatientAccessDecision.Denied));

        Result<ConsentResponse, ConsentError> result =
            await _sut.RecordAsync(_patientCode, Request("Research", DateTimeOffset.UtcNow), _user);

        Assert.IsType<ConsentAccessDeniedError>(result.Error);
    }

    [Fact]
    public async Task Revoke_requires_a_reason()
    {
        Result<ConsentResponse, ConsentError> result = await _sut.RevokeAsync(
            _patientCode, Guid.NewGuid(), new RevokeConsentRequest { Reason = "  " }, _user);

        Assert.IsType<ConsentInvalidError>(result.Error);
    }

    [Fact]
    public async Task Revoke_reports_not_found_when_nothing_was_updated_and_logs_nothing()
    {
        _repository.RevokeAsync(default, default, default!, default).ReturnsForAnyArgs(
            Result<ConsentRow?, ConsentRepositoryError>.Success(null));

        Result<ConsentResponse, ConsentError> result = await _sut.RevokeAsync(
            _patientCode, Guid.NewGuid(), new RevokeConsentRequest { Reason = "Cambió de opinión" }, _user);

        Assert.IsType<ConsentNotFoundError>(result.Error);
        await _activity.DidNotReceiveWithAnyArgs().LogAsync(default!);
    }

    [Fact]
    public async Task Own_revoke_logs_an_event_without_actor_and_without_the_reason()
    {
        _repository.RevokeAsync(default, default, default!, default).ReturnsForAnyArgs(
            Result<ConsentRow?, ConsentRepositoryError>.Success(Row()));

        Result<ConsentResponse, ConsentError> result = await _sut.RevokeOwnAsync(
            _patientCode, Guid.NewGuid(), new RevokeConsentRequest { Reason = "Cambió de opinión" });

        Assert.True(result.IsSuccess);
        await _repository.Received(1).RevokeAsync(_patientCode, Arg.Any<Guid>(), "Cambió de opinión", null);
        await _activity.Received(1).LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.ConsentRevoked && e.ActorUserCode == null
            && !e.Payload!.ToString()!.Contains("opinión")));
    }

    private static CreateConsentRequest Request(string type, DateTimeOffset at) =>
        new() { ConsentType = type, Description = "Participación en estudio", GrantedAt = at };

    private ConsentRow Row() =>
        new()
        {
            ConsentCode = Guid.NewGuid(), PatientCode = _patientCode, ConsentType = "Research",
            Description = "Participación en estudio", GrantedAt = DateTimeOffset.UtcNow, RecordedByName = "Dr X",
            RecordedAt = DateTimeOffset.UtcNow
        };
}
