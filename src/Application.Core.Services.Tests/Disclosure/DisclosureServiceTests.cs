using Application.Core.DTOs.Disclosure.Errors;
using Application.Core.DTOs.Disclosure.Request;
using Application.Core.DTOs.Disclosure.Response;
using Application.Core.DTOs.PatientAccess;
using Application.Core.DTOs.PatientAccess.Errors;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Services.Disclosure;
using BindSharp;
using Infrastructure.Core.DTOs.Disclosure;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Disclosure;
using Infrastructure.Core.Models.Disclosure;
using NSubstitute;

namespace Application.Core.Services.Tests.Disclosure;

public sealed class DisclosureServiceTests
{
    private readonly IDisclosureRepository _repository = Substitute.For<IDisclosureRepository>();
    private readonly IPatientAccessPolicy _policy = Substitute.For<IPatientAccessPolicy>();
    private readonly IRecordAccessLogger _accessLogger = Substitute.For<IRecordAccessLogger>();
    private readonly IActivityLogger _activity = Substitute.For<IActivityLogger>();
    private readonly DisclosureService _sut;
    private readonly Guid _patientCode = Guid.NewGuid();
    private readonly Guid _user = Guid.NewGuid();

    public DisclosureServiceTests()
    {
        _sut = new DisclosureService(_repository, _policy, _accessLogger, _activity);
        _policy.EvaluateAsync(Arg.Any<Guid>(), Arg.Any<PatientDataScope>())
            .Returns(Result<PatientAccessDecision, PatientAccessError>.Success(PatientAccessDecision.Allowed));
    }

    [Fact]
    public async Task Record_rejects_a_future_date()
    {
        Result<DisclosureResponse, DisclosureError> result = await _sut.RecordAsync(_patientCode,
            Request(DateTimeOffset.UtcNow.AddDays(1), "Court"), _user);

        Assert.IsType<DisclosureInvalidError>(result.Error);
        await _repository.DidNotReceiveWithAnyArgs().InsertAsync(default, default, default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task Record_rejects_an_unknown_recipient_type()
    {
        Result<DisclosureResponse, DisclosureError> result = await _sut.RecordAsync(_patientCode,
            Request(DateTimeOffset.UtcNow, "Vecino"), _user);

        Assert.IsType<DisclosureInvalidError>(result.Error);
    }

    [Fact]
    public async Task Record_logs_the_recipient_type_but_never_the_name_or_description()
    {
        DateTimeOffset when = DateTimeOffset.UtcNow.AddHours(-1);
        _repository.InsertAsync(_patientCode, when, "Juzgado Civil", "Court", "Orden judicial", "Resumen de atención", _user)
            .Returns(Result<DisclosureRow?, DisclosureRepositoryError>.Success(Row(when)));

        Result<DisclosureResponse, DisclosureError> result =
            await _sut.RecordAsync(_patientCode, Request(when, "Court"), _user);

        Assert.True(result.IsSuccess);
        await _activity.Received(1).LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.DisclosureRecorded
            && e.ActorUserCode == _user
            && !e.Payload!.ToString()!.Contains("Juzgado")
            && !e.Payload!.ToString()!.Contains("Resumen")));
    }

    [Fact]
    public async Task Record_is_denied_when_the_policy_denies_the_patient()
    {
        _policy.EvaluateAsync(_patientCode, PatientDataScope.Administrative)
            .Returns(Result<PatientAccessDecision, PatientAccessError>.Success(PatientAccessDecision.Denied));

        Result<DisclosureResponse, DisclosureError> result = await _sut.RecordAsync(_patientCode,
            Request(DateTimeOffset.UtcNow, "Court"), _user);

        Assert.IsType<DisclosureAccessDeniedError>(result.Error);
    }

    [Fact]
    public async Task Record_reports_a_missing_patient()
    {
        _repository.InsertAsync(default, default, default!, default!, default!, default!, default).ReturnsForAnyArgs(
            Result<DisclosureRow?, DisclosureRepositoryError>.Success(null));

        Result<DisclosureResponse, DisclosureError> result = await _sut.RecordAsync(_patientCode,
            Request(DateTimeOffset.UtcNow, "Court"), _user);

        Assert.IsType<DisclosurePatientNotFoundError>(result.Error);
    }

    private static CreateDisclosureRequest Request(DateTimeOffset at, string type) =>
        new()
        {
            DisclosedAt = at, RecipientName = "Juzgado Civil", RecipientType = type,
            Purpose = "Orden judicial", Description = "Resumen de atención"
        };

    private DisclosureRow Row(DateTimeOffset at) =>
        new()
        {
            DisclosureCode = Guid.NewGuid(), PatientCode = _patientCode, DisclosedAt = at,
            RecipientName = "Juzgado Civil", RecipientType = "Court", Purpose = "Orden judicial",
            Description = "Resumen de atención", RecordedByName = "Dr X", RecordedAt = DateTimeOffset.UtcNow
        };
}
