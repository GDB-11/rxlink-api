using Application.Core.DTOs.PatientAccess;
using Application.Core.DTOs.PatientAccess.Errors;
using Application.Core.DTOs.PrivacyNotice.Errors;
using Application.Core.DTOs.PrivacyNotice.Request;
using Application.Core.DTOs.PrivacyNotice.Response;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Services.PrivacyNotice;
using BindSharp;
using Infrastructure.Core.DTOs.PrivacyNotice;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.PrivacyNotice;
using Infrastructure.Core.Models.PrivacyNotice;
using NSubstitute;

namespace Application.Core.Services.Tests.PrivacyNotice;

public sealed class PrivacyNoticeServiceTests
{
    private readonly IPrivacyNoticeRepository _repository = Substitute.For<IPrivacyNoticeRepository>();
    private readonly IPatientAccessPolicy _policy = Substitute.For<IPatientAccessPolicy>();
    private readonly IRecordAccessLogger _accessLogger = Substitute.For<IRecordAccessLogger>();
    private readonly IActivityLogger _activity = Substitute.For<IActivityLogger>();
    private readonly PrivacyNoticeService _sut;
    private readonly Guid _patientCode = Guid.NewGuid();
    private readonly Guid _user = Guid.NewGuid();
    private readonly PrivacyNoticeRow _notice = new()
    {
        PrivacyNoticeCode = Guid.NewGuid(), Version = 3, Title = "Aviso", Body = "Texto completo del aviso de privacidad",
        EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow), PublishedAt = DateTimeOffset.UtcNow
    };

    public PrivacyNoticeServiceTests()
    {
        _sut = new PrivacyNoticeService(_repository, _policy, _accessLogger, _activity);
        _policy.EvaluateAsync(Arg.Any<Guid>(), Arg.Any<PatientDataScope>())
            .Returns(Result<PatientAccessDecision, PatientAccessError>.Success(PatientAccessDecision.Allowed));
        _repository.GetCurrentAsync()
            .Returns(Result<PrivacyNoticeRow?, PrivacyNoticeRepositoryError>.Success(_notice));
    }

    [Fact]
    public async Task Publish_rejects_a_too_short_body()
    {
        Result<PrivacyNoticeResponse, PrivacyNoticeError> result = await _sut.PublishAsync(
            new PublishPrivacyNoticeRequest { Title = "Aviso", Body = "corto", EffectiveFrom = DateOnly.MaxValue }, _user);

        Assert.IsType<PrivacyNoticeInvalidError>(result.Error);
    }

    [Fact]
    public async Task Publish_rejects_a_past_effective_date()
    {
        Result<PrivacyNoticeResponse, PrivacyNoticeError> result = await _sut.PublishAsync(
            new PublishPrivacyNoticeRequest
            {
                Title = "Aviso", Body = "Texto completo del aviso de privacidad", EffectiveFrom = new DateOnly(2020, 1, 1)
            }, _user);

        Assert.IsType<PrivacyNoticeInvalidError>(result.Error);
    }

    [Fact]
    public async Task Publish_logs_the_version_but_never_the_text()
    {
        _repository.PublishAsync(default!, default!, default, default).ReturnsForAnyArgs(
            Result<PrivacyNoticeRow?, PrivacyNoticeRepositoryError>.Success(_notice));

        Result<PrivacyNoticeResponse, PrivacyNoticeError> result = await _sut.PublishAsync(
            new PublishPrivacyNoticeRequest
            {
                Title = "Aviso", Body = "Texto completo del aviso de privacidad", EffectiveFrom = DateOnly.MaxValue
            }, _user);

        Assert.True(result.IsSuccess);
        await _activity.Received(1).LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.PrivacyNoticePublished
            && !e.Payload!.ToString()!.Contains("Texto")));
    }

    [Fact]
    public async Task Acknowledge_logs_an_event_only_when_a_row_was_written()
    {
        _repository.AcknowledgeCurrentAsync(_patientCode, _user)
            .Returns(Result<int, PrivacyNoticeRepositoryError>.Success(1));
        _repository.GetAcknowledgmentsAsync(_patientCode).Returns(
            Result<IEnumerable<PrivacyNoticeAcknowledgmentRow>, PrivacyNoticeRepositoryError>.Success(
                [Ack()]));

        Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError> result =
            await _sut.AcknowledgeAsync(_patientCode, _user);

        Assert.True(result.Value!.CurrentAcknowledged);
        await _activity.Received(1).LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.PrivacyNoticeAcknowledged && e.ActorUserCode == _user));
    }

    [Fact]
    public async Task Acknowledge_twice_is_an_idempotent_success_without_a_second_event()
    {
        _repository.AcknowledgeCurrentAsync(_patientCode, _user)
            .Returns(Result<int, PrivacyNoticeRepositoryError>.Success(0));
        _repository.GetAcknowledgmentsAsync(_patientCode).Returns(
            Result<IEnumerable<PrivacyNoticeAcknowledgmentRow>, PrivacyNoticeRepositoryError>.Success([Ack()]));

        Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError> result =
            await _sut.AcknowledgeAsync(_patientCode, _user);

        Assert.True(result.IsSuccess);
        await _activity.DidNotReceiveWithAnyArgs().LogAsync(default!);
    }

    [Fact]
    public async Task Acknowledge_reports_a_missing_patient_when_nothing_is_written_nor_acknowledged()
    {
        _repository.AcknowledgeCurrentAsync(_patientCode, null)
            .Returns(Result<int, PrivacyNoticeRepositoryError>.Success(0));
        _repository.GetAcknowledgmentsAsync(_patientCode).Returns(
            Result<IEnumerable<PrivacyNoticeAcknowledgmentRow>, PrivacyNoticeRepositoryError>.Success([]));

        Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError> result =
            await _sut.AcknowledgeOwnAsync(_patientCode);

        Assert.IsType<PrivacyNoticePatientNotFoundError>(result.Error);
    }

    [Fact]
    public async Task Acknowledge_fails_when_no_notice_is_in_force()
    {
        _repository.GetCurrentAsync()
            .Returns(Result<PrivacyNoticeRow?, PrivacyNoticeRepositoryError>.Success(null));
        _repository.AcknowledgeCurrentAsync(_patientCode, null)
            .Returns(Result<int, PrivacyNoticeRepositoryError>.Success(0));
        _repository.GetAcknowledgmentsAsync(_patientCode).Returns(
            Result<IEnumerable<PrivacyNoticeAcknowledgmentRow>, PrivacyNoticeRepositoryError>.Success([]));

        Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError> result =
            await _sut.AcknowledgeOwnAsync(_patientCode);

        Assert.IsType<PrivacyNoticeNotPublishedError>(result.Error);
    }

    [Fact]
    public async Task Status_is_denied_when_the_policy_denies_the_patient()
    {
        _policy.EvaluateAsync(_patientCode, PatientDataScope.Administrative)
            .Returns(Result<PatientAccessDecision, PatientAccessError>.Success(PatientAccessDecision.Denied));

        Result<PatientPrivacyNoticeStatusResponse, PrivacyNoticeError> result = await _sut.GetStatusAsync(_patientCode);

        Assert.IsType<PrivacyNoticeAccessDeniedError>(result.Error);
    }

    private PrivacyNoticeAcknowledgmentRow Ack() =>
        new()
        {
            PrivacyNoticeCode = _notice.PrivacyNoticeCode, Version = _notice.Version, Title = _notice.Title,
            AcknowledgedAt = DateTimeOffset.UtcNow
        };
}
