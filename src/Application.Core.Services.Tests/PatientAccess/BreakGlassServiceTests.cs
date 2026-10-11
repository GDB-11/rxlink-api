using Application.Core.DTOs.PatientAccess.Errors;
using Application.Core.DTOs.PatientAccess.Request;
using Application.Core.DTOs.Report.Request;
using Application.Core.Services.PatientAccess;
using BindSharp;
using Infrastructure.Core.DTOs.PatientAccess;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.PatientAccess;
using Infrastructure.Core.Models.PatientAccess;
using NSubstitute;

namespace Application.Core.Services.Tests.PatientAccess;

public sealed class BreakGlassServiceTests
{
    private readonly IPatientAccessRepository _repository = Substitute.For<IPatientAccessRepository>();
    private readonly IActivityLogger _activityLogger = Substitute.For<IActivityLogger>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly BreakGlassService _sut;
    private readonly Guid _userCode = Guid.NewGuid();

    public BreakGlassServiceTests()
    {
        _sut = new BreakGlassService(_repository, _activityLogger, _userContext);
        _userContext.Role.Returns("Enfermero");
        _userContext.UserCode.Returns(_userCode);
    }

    private static BreakGlassRequest Request(string mrn = " PAC-202601-00001 ", string reason = " Paciente inconsciente en urgencias ") =>
        new() { MedicalRecordNumber = mrn, Reason = reason };

    private void GivenGrant(BreakGlassGrantRow? row) =>
        _repository.GrantBreakGlassAsync(_userCode, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns(Task.FromResult(Result<BreakGlassGrantRow?, PatientAccessRepositoryError>.Success(row)));

    [Fact]
    public async Task RequestAsync_Nurse_TrimsInputAndGrantsFourHours()
    {
        var patientCode = Guid.NewGuid();
        var expires = DateTimeOffset.UtcNow.AddHours(4);
        GivenGrant(new BreakGlassGrantRow
            { PatientCode = patientCode, InCareTeam = false, GrantCode = Guid.NewGuid(), ExpiresAt = expires });

        var result = await _sut.RequestAsync(Request());

        Assert.True(result.IsSuccess);
        Assert.Equal(patientCode, result.Value.PatientCode);
        Assert.Equal(expires, result.Value.ExpiresAt);
        await _repository.Received(1).GrantBreakGlassAsync(
            _userCode, "PAC-202601-00001", "Paciente inconsciente en urgencias", 240);
    }

    [Fact]
    public async Task RequestAsync_PatientAlreadyInCareTeam_ReturnsNoExpiry()
    {
        var patientCode = Guid.NewGuid();
        GivenGrant(new BreakGlassGrantRow { PatientCode = patientCode, InCareTeam = true });

        var result = await _sut.RequestAsync(Request());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.ExpiresAt);
    }

    [Fact]
    public async Task RequestAsync_UnknownMedicalRecordNumber_ReturnsNotFound()
    {
        GivenGrant(null);

        var result = await _sut.RequestAsync(Request());

        Assert.True(result.IsFailure);
        Assert.IsType<BreakGlassPatientNotFoundError>(result.Error);
    }

    [Theory]
    [InlineData("Doctor")]
    [InlineData("Administrador")]
    [InlineData("Patient")]
    public async Task RequestAsync_NonNurse_IsNotPermitted(string role)
    {
        _userContext.Role.Returns(role);

        var result = await _sut.RequestAsync(Request());

        Assert.True(result.IsFailure);
        Assert.IsType<PatientAccessNotPermittedError>(result.Error);
        await _repository.DidNotReceiveWithAnyArgs()
            .GrantBreakGlassAsync(default, default!, default!, default);
    }

    [Fact]
    public async Task GetReportAsync_InvertedRange_ReturnsInvalidRange()
    {
        var result = await _sut.GetReportAsync(
            new ReportDateRangeRequest { From = new DateOnly(2026, 2, 1), To = new DateOnly(2026, 1, 1) });

        Assert.True(result.IsFailure);
        Assert.IsType<BreakGlassReportInvalidRangeError>(result.Error);
    }

    [Fact]
    public async Task GetReportAsync_ValidRange_ReturnsRowsAndLogsReportViewed()
    {
        var row = new BreakGlassReportRow
        {
            GrantCode = Guid.NewGuid(), UserCode = _userCode, Username = "enf1", PatientCode = Guid.NewGuid(),
            MedicalRecordNumber = "PAC-202601-00001", Reason = "Emergencia en sala",
            GrantedAt = DateTimeOffset.UtcNow, ExpiresAt = DateTimeOffset.UtcNow.AddHours(4)
        };
        _repository.GetBreakGlassReportAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
            .Returns(Task.FromResult(Result<IEnumerable<BreakGlassReportRow>, PatientAccessRepositoryError>
                .Success([row])));

        var result = await _sut.GetReportAsync(
            new ReportDateRangeRequest { From = new DateOnly(2026, 1, 1), To = new DateOnly(2026, 1, 31) });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("enf1", result.Value[0].Username);
        await _activityLogger.Received(1).LogAsync(Arg.Is<ActivityEvent>(e => e.EventType == ActivityEventTypes.ReportViewed));
    }
}
