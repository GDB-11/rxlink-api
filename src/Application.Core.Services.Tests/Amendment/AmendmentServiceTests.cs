using Application.Core.DTOs.Amendment.Errors;
using Application.Core.DTOs.Amendment.Request;
using Application.Core.DTOs.Amendment.Response;
using Application.Core.DTOs.PatientAccess;
using Application.Core.DTOs.PatientAccess.Errors;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Services.Amendment;
using BindSharp;
using Infrastructure.Core.DTOs.Amendment;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Amendment;
using Infrastructure.Core.Models.Amendment;
using NSubstitute;

namespace Application.Core.Services.Tests.Amendment;

public sealed class AmendmentServiceTests
{
    private readonly IAmendmentRepository _repository = Substitute.For<IAmendmentRepository>();
    private readonly IPatientAccessPolicy _policy = Substitute.For<IPatientAccessPolicy>();
    private readonly IRecordAccessLogger _accessLogger = Substitute.For<IRecordAccessLogger>();
    private readonly IActivityLogger _activity = Substitute.For<IActivityLogger>();
    private readonly AmendmentService _sut;
    private readonly Guid _patientCode = Guid.NewGuid();
    private readonly Guid _requestCode = Guid.NewGuid();
    private readonly Guid _reviewer = Guid.NewGuid();

    public AmendmentServiceTests()
    {
        _sut = new AmendmentService(_repository, _policy, _accessLogger, _activity);
        _policy.EvaluateAsync(Arg.Any<Guid>(), Arg.Any<PatientDataScope>())
            .Returns(Result<PatientAccessDecision, PatientAccessError>.Success(PatientAccessDecision.Allowed));
    }

    [Fact]
    public async Task Create_rejects_an_unknown_area_without_touching_the_database()
    {
        Result<AmendmentResponse, AmendmentError> result = await _sut.CreateAsync(_patientCode,
            new CreateAmendmentRequest { TargetArea = "Nope", RequestedChange = "Mi dirección está mal escrita" });

        Assert.IsType<AmendmentInvalidError>(result.Error);
        await _repository.DidNotReceiveWithAnyArgs().InsertAsync(default, default!, default!);
    }

    [Fact]
    public async Task Create_logs_the_area_but_not_the_patients_words()
    {
        _repository.InsertAsync(_patientCode, "Profile", "Mi dirección está mal escrita")
            .Returns(Result<AmendmentRow?, AmendmentRepositoryError>.Success(Row("Pendiente")));

        Result<AmendmentResponse, AmendmentError> result = await _sut.CreateAsync(_patientCode,
            new CreateAmendmentRequest { TargetArea = "Profile", RequestedChange = "Mi dirección está mal escrita" });

        Assert.True(result.IsSuccess);
        await _activity.Received(1).LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.AmendmentRequested
            && e.ActorUserCode == null
            && !e.Payload!.ToString()!.Contains("dirección")));
    }

    [Fact]
    public async Task Decide_accept_needs_the_text_to_append()
    {
        Result<AmendmentResponse, AmendmentError> result = await _sut.DecideAsync(_requestCode,
            new DecideAmendmentRequest { Accepted = true, Reason = "Corresponde" }, _reviewer);

        Assert.IsType<AmendmentInvalidError>(result.Error);
        await _repository.DidNotReceiveWithAnyArgs().DecideAsync(default, default, default!, default, default);
    }

    [Fact]
    public async Task Decide_refuses_a_request_that_is_already_decided()
    {
        _repository.GetByCodeAsync(_requestCode)
            .Returns(Result<AmendmentRow?, AmendmentRepositoryError>.Success(Row("Denegada")));

        Result<AmendmentResponse, AmendmentError> result = await _sut.DecideAsync(_requestCode,
            new DecideAmendmentRequest { Accepted = false, Reason = "No procede" }, _reviewer);

        Assert.IsType<AmendmentAlreadyDecidedError>(result.Error);
        await _repository.DidNotReceiveWithAnyArgs().DecideAsync(default, default, default!, default, default);
    }

    [Fact]
    public async Task Decide_reports_a_lost_race_as_already_decided()
    {
        _repository.GetByCodeAsync(_requestCode)
            .Returns(Result<AmendmentRow?, AmendmentRepositoryError>.Success(Row("Pendiente")));
        _repository.DecideAsync(_requestCode, false, "No procede", null, _reviewer)
            .Returns(Result<AmendmentRow?, AmendmentRepositoryError>.Success(null));

        Result<AmendmentResponse, AmendmentError> result = await _sut.DecideAsync(_requestCode,
            new DecideAmendmentRequest { Accepted = false, Reason = "No procede" }, _reviewer);

        Assert.IsType<AmendmentAlreadyDecidedError>(result.Error);
    }

    [Fact]
    public async Task Decide_is_denied_when_the_policy_denies_the_patient()
    {
        _repository.GetByCodeAsync(_requestCode)
            .Returns(Result<AmendmentRow?, AmendmentRepositoryError>.Success(Row("Pendiente")));
        _policy.EvaluateAsync(_patientCode, PatientDataScope.Clinical)
            .Returns(Result<PatientAccessDecision, PatientAccessError>.Success(PatientAccessDecision.Denied));

        Result<AmendmentResponse, AmendmentError> result = await _sut.DecideAsync(_requestCode,
            new DecideAmendmentRequest { Accepted = false, Reason = "No procede" }, _reviewer);

        Assert.IsType<AmendmentAccessDeniedError>(result.Error);
    }

    [Fact]
    public async Task Decide_accept_logs_the_decision_without_the_text()
    {
        _repository.GetByCodeAsync(_requestCode)
            .Returns(Result<AmendmentRow?, AmendmentRepositoryError>.Success(Row("Pendiente")));
        _repository.DecideAsync(_requestCode, true, "Corresponde", "Fecha corregida", _reviewer)
            .Returns(Result<AmendmentRow?, AmendmentRepositoryError>.Success(Row("Aceptada")));

        Result<AmendmentResponse, AmendmentError> result = await _sut.DecideAsync(_requestCode,
            new DecideAmendmentRequest { Accepted = true, Reason = "Corresponde", AmendmentText = "Fecha corregida" },
            _reviewer);

        Assert.True(result.IsSuccess);
        await _activity.Received(1).LogAsync(Arg.Is<ActivityEvent>(e =>
            e.EventType == ActivityEventTypes.AmendmentDecided
            && e.ActorUserCode == _reviewer
            && !e.Payload!.ToString()!.Contains("Fecha corregida")));
    }

    [Fact]
    public async Task List_rejects_an_unknown_status()
    {
        Result<AmendmentPageResponse, AmendmentError> result =
            await _sut.GetPageAsync(new AmendmentPageRequest { Status = "Otro" });

        Assert.IsType<AmendmentInvalidError>(result.Error);
    }

    private AmendmentRow Row(string status) =>
        new()
        {
            AmendmentRequestCode = _requestCode, PatientCode = _patientCode, PatientName = "A B",
            MedicalRecordNumber = "HC1", TargetArea = "Profile", RequestedChange = "Mi dirección está mal escrita",
            Status = status, RequestedAt = DateTimeOffset.UtcNow
        };
}
