using Application.Core.DTOs.Allergy.Errors;
using Application.Core.DTOs.Allergy.Request;
using Application.Core.Services.Allergy;
using BindSharp;
using Infrastructure.Core.DTOs.Allergy;
using Infrastructure.Core.Interfaces.Allergy;
using Infrastructure.Core.Models.Allergy;
using NSubstitute;

namespace Application.Core.Services.Tests.Allergy;

public sealed class AllergyServiceTests
{
    private readonly IAllergyRepository _repository = Substitute.For<IAllergyRepository>();
    private readonly AllergyService _sut;

    public AllergyServiceTests() => _sut = new AllergyService(_repository);

    private static AllergyRow MakeRow(Guid? code = null, long totalCount = 1) => new()
    {
        AllergyCode = code ?? Guid.NewGuid(),
        Name = "Penicilina",
        Description = "Alergia a la penicilina",
        IsActive = true,
        TotalCount = totalCount
    };

    // ── GetPageAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPageAsync_RepositoryReturnsRows_BuildsPageResponseCorrectly()
    {
        var row = MakeRow(totalCount: 5);
        _repository.GetPageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Task.FromResult(
                Result<IEnumerable<AllergyRow>, AllergyRepositoryError>.Success([row])));

        var result = await _sut.GetPageAsync(new AllergyPageRequest { Page = 1, PageSize = 5 });

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.TotalCount);
        Assert.Equal(1, result.Value.TotalPages);
        Assert.Single(result.Value.Items);
        Assert.Equal(row.AllergyCode, result.Value.Items[0].AllergyCode);
        Assert.Equal(row.Name, result.Value.Items[0].Name);
    }

    [Fact]
    public async Task GetPageAsync_EmptyRows_ReturnsTotalCountAndTotalPagesZero()
    {
        _repository.GetPageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Task.FromResult(
                Result<IEnumerable<AllergyRow>, AllergyRepositoryError>.Success(
                    Enumerable.Empty<AllergyRow>())));

        var result = await _sut.GetPageAsync(new AllergyPageRequest { Page = 1, PageSize = 20 });

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(0, result.Value.TotalPages);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task GetPageAsync_ComputesOffsetFromPageAndPageSize()
    {
        _repository.GetPageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Task.FromResult(
                Result<IEnumerable<AllergyRow>, AllergyRepositoryError>.Success(
                    Enumerable.Empty<AllergyRow>())));

        await _sut.GetPageAsync(new AllergyPageRequest { Page = 3, PageSize = 10 });

        await _repository.Received(1).GetPageAsync(20, 10, Arg.Any<string?>());
    }

    [Fact]
    public async Task GetPageAsync_RepositoryFails_ReturnsAllergyDataAccessError()
    {
        _repository.GetPageAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>())
            .Returns(Task.FromResult(
                Result<IEnumerable<AllergyRow>, AllergyRepositoryError>.Failure(
                    new GetAllergiesPageError())));

        var result = await _sut.GetPageAsync(new AllergyPageRequest { Page = 1, PageSize = 20 });

        Assert.True(result.IsFailure);
        Assert.IsType<AllergyDataAccessError>(result.Error);
    }

    // ── CreateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_RepositoryReturnsRow_MapsToAllergyResponse()
    {
        var row = MakeRow();
        _repository.InsertAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult(
                Result<AllergyRow?, AllergyRepositoryError>.Success(row)));

        var result = await _sut.CreateAsync(new CreateAllergyRequest
            { Name = "Penicilina", Description = "Descripción" });

        Assert.True(result.IsSuccess);
        Assert.Equal(row.AllergyCode, result.Value.AllergyCode);
        Assert.Equal(row.Name, result.Value.Name);
        Assert.Equal(row.IsActive, result.Value.IsActive);
    }

    [Fact]
    public async Task CreateAsync_RepositoryReturnsNull_ReturnsAllergyDataAccessError()
    {
        _repository.InsertAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult(
                Result<AllergyRow?, AllergyRepositoryError>.Success(null)));

        var result = await _sut.CreateAsync(new CreateAllergyRequest { Name = "Penicilina" });

        Assert.True(result.IsFailure);
        Assert.IsType<AllergyDataAccessError>(result.Error);
    }

    [Fact]
    public async Task CreateAsync_RepositoryFails_ReturnsAllergyDataAccessError()
    {
        _repository.InsertAsync(Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult(
                Result<AllergyRow?, AllergyRepositoryError>.Failure(new InsertAllergyError())));

        var result = await _sut.CreateAsync(new CreateAllergyRequest { Name = "Penicilina" });

        Assert.True(result.IsFailure);
        Assert.IsType<AllergyDataAccessError>(result.Error);
    }

    // ── UpdateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_RepositoryReturnsRow_MapsToAllergyResponse()
    {
        var code = Guid.NewGuid();
        var row = MakeRow(code);
        _repository.UpdateAsync(code, Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult(
                Result<AllergyRow?, AllergyRepositoryError>.Success(row)));

        var result = await _sut.UpdateAsync(code, new UpdateAllergyRequest { Name = "Pólvora" });

        Assert.True(result.IsSuccess);
        Assert.Equal(code, result.Value.AllergyCode);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryReturnsNull_ReturnsAllergyNotFoundError()
    {
        var code = Guid.NewGuid();
        _repository.UpdateAsync(code, Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult(
                Result<AllergyRow?, AllergyRepositoryError>.Success(null)));

        var result = await _sut.UpdateAsync(code, new UpdateAllergyRequest { Name = "Pólvora" });

        Assert.True(result.IsFailure);
        Assert.IsType<AllergyNotFoundError>(result.Error);
    }

    [Fact]
    public async Task UpdateAsync_RepositoryFails_ReturnsAllergyDataAccessError()
    {
        var code = Guid.NewGuid();
        _repository.UpdateAsync(code, Arg.Any<string>(), Arg.Any<string?>())
            .Returns(Task.FromResult(
                Result<AllergyRow?, AllergyRepositoryError>.Failure(new UpdateAllergyError())));

        var result = await _sut.UpdateAsync(code, new UpdateAllergyRequest { Name = "Pólvora" });

        Assert.True(result.IsFailure);
        Assert.IsType<AllergyDataAccessError>(result.Error);
    }

    // ── DeactivateAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateAsync_RepositoryAffectsOneRow_ReturnsUnit()
    {
        var code = Guid.NewGuid();
        var userCode = Guid.NewGuid();
        _repository.DeactivateAsync(code, userCode)
            .Returns(Task.FromResult(Result<int, AllergyRepositoryError>.Success(1)));

        var result = await _sut.DeactivateAsync(code, userCode);

        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);
    }

    [Fact]
    public async Task DeactivateAsync_RepositoryAffectsZeroRows_ReturnsAllergyNotFoundError()
    {
        var code = Guid.NewGuid();
        var userCode = Guid.NewGuid();
        _repository.DeactivateAsync(code, userCode)
            .Returns(Task.FromResult(Result<int, AllergyRepositoryError>.Success(0)));

        var result = await _sut.DeactivateAsync(code, userCode);

        Assert.True(result.IsFailure);
        Assert.IsType<AllergyNotFoundError>(result.Error);
    }

    [Fact]
    public async Task DeactivateAsync_RepositoryFails_ReturnsFailure()
    {
        // BindSharp 2.1.0: EnsureAsync always evaluates predicate(default(int)=0) on Failure,
        // so the exact error type is overwritten — we can only assert IsFailure.
        var code = Guid.NewGuid();
        var userCode = Guid.NewGuid();
        _repository.DeactivateAsync(code, userCode)
            .Returns(Task.FromResult(
                Result<int, AllergyRepositoryError>.Failure(new DeactivateAllergyError())));

        var result = await _sut.DeactivateAsync(code, userCode);

        Assert.True(result.IsFailure);
    }

    // ── ActivateAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateAsync_RepositoryAffectsOneRow_ReturnsUnit()
    {
        var code = Guid.NewGuid();
        _repository.ActivateAsync(code)
            .Returns(Task.FromResult(Result<int, AllergyRepositoryError>.Success(1)));

        var result = await _sut.ActivateAsync(code, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal(Unit.Value, result.Value);
    }

    [Fact]
    public async Task ActivateAsync_RepositoryAffectsZeroRows_ReturnsAllergyNotFoundError()
    {
        var code = Guid.NewGuid();
        _repository.ActivateAsync(code)
            .Returns(Task.FromResult(Result<int, AllergyRepositoryError>.Success(0)));

        var result = await _sut.ActivateAsync(code, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.IsType<AllergyNotFoundError>(result.Error);
    }

    [Fact]
    public async Task ActivateAsync_RepositoryFails_ReturnsFailure()
    {
        // BindSharp 2.1.0: EnsureAsync evaluates predicate(0) on int Failure,
        // so the exact error type is overwritten — we can only assert IsFailure.
        var code = Guid.NewGuid();
        _repository.ActivateAsync(code)
            .Returns(Task.FromResult(
                Result<int, AllergyRepositoryError>.Failure(new DeactivateAllergyError())));

        var result = await _sut.ActivateAsync(code, Guid.NewGuid());

        Assert.True(result.IsFailure);
    }
}
