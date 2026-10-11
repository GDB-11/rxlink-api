using Application.Core.DTOs.Clinic.Errors;
using Application.Core.DTOs.Clinic.Request;
using Application.Core.Services.Clinic;
using BindSharp;
using Infrastructure.Core.DTOs.Clinic;
using Infrastructure.Core.Interfaces.Clinic;
using Infrastructure.Core.Models.Clinic;
using NSubstitute;

namespace Application.Core.Services.Tests.Clinic;

public sealed class ClinicServiceTests
{
    private readonly IClinicRepository _repository = Substitute.For<IClinicRepository>();
    private readonly ClinicService _sut;

    public ClinicServiceTests() => _sut = new ClinicService(_repository);

    private static ClinicRow Row() => new()
    {
        ClinicCode = Guid.NewGuid(), Name = "Sede Principal", UpdatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task UpdateDefaultAsync_TrimsValuesAndTurnsBlankIntoNull()
    {
        _repository.UpdateDefaultAsync(default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(Result<ClinicRow?, ClinicRepositoryError>.Success(Row())));

        var result = await _sut.UpdateDefaultAsync(new UpdateClinicRequest
        {
            Address = "  Av. Lima 123 ", District = "   ", Phone = " +51 999 999 999 "
        });

        Assert.True(result.IsSuccess);
        await _repository.Received(1).UpdateDefaultAsync(
            "Av. Lima 123", null, null, null, "+51 999 999 999", null, null, null);
    }

    [Fact]
    public async Task UpdateDefaultAsync_NoDefaultClinic_ReturnsNotFound()
    {
        _repository.UpdateDefaultAsync(default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(Task.FromResult(Result<ClinicRow?, ClinicRepositoryError>.Success(null)));

        var result = await _sut.UpdateDefaultAsync(new UpdateClinicRequest());

        Assert.True(result.IsFailure);
        Assert.IsType<ClinicNotFoundError>(result.Error);
    }

    [Fact]
    public async Task GetDefaultAsync_RepositoryFailure_ReturnsDataAccessError()
    {
        _repository.GetDefaultAsync().Returns(Task.FromResult(
            Result<ClinicRow?, ClinicRepositoryError>.Failure(new GetClinicError("db"))));

        var result = await _sut.GetDefaultAsync();

        Assert.True(result.IsFailure);
        Assert.IsType<ClinicDataAccessError>(result.Error);
    }
}
