using System.Collections.Frozen;
using Application.Core.DTOs.Specialty.Errors;
using Application.Core.Services.Specialty;
using BindSharp;
using Infrastructure.Core.DTOs.PartnerCatalog;
using Infrastructure.Core.DTOs.Specialty;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.PartnerCatalog;
using Infrastructure.Core.Interfaces.Specialty;
using Infrastructure.Core.Models.Catalog;
using Infrastructure.Core.Models.PartnerCatalog;
using Infrastructure.Core.Models.Specialty;
using NSubstitute;

namespace Application.Core.Services.Tests.Specialty;

public sealed class SpecialtyServiceTests
{
    private readonly ISpecialtyRepository _repository = Substitute.For<ISpecialtyRepository>();
    private readonly ICatalogSnapshotStore _store = Substitute.For<ICatalogSnapshotStore>();
    private readonly IPartnerCatalogRepository _partner = Substitute.For<IPartnerCatalogRepository>();
    private readonly SpecialtyService _sut;

    private readonly Guid _cardio = Guid.NewGuid();
    private readonly Guid _pediatria = Guid.NewGuid();
    private readonly Guid _disabled = Guid.NewGuid();
    private readonly Guid _inactive = Guid.NewGuid();

    public SpecialtyServiceTests()
    {
        _store.Current.Returns(new CatalogSnapshot
        {
            Version = 1, SchemaVersion = 1, LoadedAt = DateTimeOffset.UtcNow,
            DocumentTypes = FrozenDictionary<Guid, DocumentTypeCatalogRow>.Empty,
            Sexes = FrozenDictionary<Guid, SexCatalogRow>.Empty,
            Specialties = new Dictionary<Guid, SpecialtyCatalogRow>
            {
                [_pediatria] = new() { Code = _pediatria, Name = "Pediatría", IsActive = true },
                [_cardio] = new() { Code = _cardio, Name = "Cardiología", IsActive = true },
                [_disabled] = new() { Code = _disabled, Name = "Deshabilitada", IsActive = true },
                [_inactive] = new() { Code = _inactive, Name = "Inactiva", IsActive = false }
            }.ToFrozenDictionary(),
            PharmaceuticalForms = FrozenDictionary<Guid, PharmaceuticalFormCatalogRow>.Empty,
            AdministrationRoutes = FrozenDictionary<Guid, AdministrationRouteCatalogRow>.Empty,
            Frequencies = FrozenDictionary<Guid, FrequencyCatalogRow>.Empty,
            Allergies = FrozenDictionary<Guid, AllergyCatalogRow>.Empty,
            AllergySeverities = FrozenDictionary<Guid, AllergySeverityCatalogRow>.Empty,
            Medications = FrozenDictionary<Guid, MedicationCatalogRow>.Empty,
            Insurances = FrozenDictionary<Guid, InsuranceCatalogRow>.Empty
        });

        _partner.GetAllSpecialtiesAsync().Returns(
            Result<IReadOnlyList<PartnerSpecialtyRow>, PartnerCatalogRepositoryError>.Success(
            [
                new() { SpecialtyCode = _cardio, IsEnabled = true, PriceInPerson = 1, PriceVirtual = 1 },
                new() { SpecialtyCode = _pediatria, IsEnabled = true, PriceInPerson = 1, PriceVirtual = 1 },
                new() { SpecialtyCode = _disabled, IsEnabled = false, PriceInPerson = 1, PriceVirtual = 1 },
                new() { SpecialtyCode = _inactive, IsEnabled = true, PriceInPerson = 1, PriceVirtual = 1 }
            ]));

        _sut = new SpecialtyService(_repository, _store, _partner);
    }

    [Fact]
    public async Task GetAll_ListsOnlyActiveEnabledSpecialties_WithCountsAndSnapshotNames()
    {
        _repository.GetDoctorCountsAsync().Returns(
            Result<IReadOnlyList<SpecialtyDoctorCountRow>, SpecialtyRepositoryError>.Success(
            [
                new() { SpecialtyCode = _cardio, DoctorCount = 3 },
                new() { SpecialtyCode = _disabled, DoctorCount = 9 }
            ]));

        var result = await _sut.GetAllActiveWithDoctorCountAsync();

        Assert.True(result.IsSuccess);
        var items = result.Value.ToList();
        Assert.Equal(["Cardiología", "Pediatría"], items.Select(i => i.Name));
        Assert.Equal([3, 0], items.Select(i => i.DoctorCount));
    }

    [Fact]
    public async Task GetAll_WhenSnapshotMissing_ReturnsCatalogUnavailable()
    {
        _store.Current.Returns((CatalogSnapshot?)null);

        var result = await _sut.GetAllActiveWithDoctorCountAsync();

        Assert.False(result.IsSuccess);
        Assert.IsType<SpecialtyCatalogUnavailableError>(result.Error);
    }

    [Fact]
    public async Task GetAll_WhenRepositoryFails_ReturnsDataAccessError()
    {
        _repository.GetDoctorCountsAsync().Returns(
            Result<IReadOnlyList<SpecialtyDoctorCountRow>, SpecialtyRepositoryError>.Failure(
                new GetActiveSpecialtiesWithCountError("boom")));

        var result = await _sut.GetAllActiveWithDoctorCountAsync();

        Assert.IsType<SpecialtyDataAccessError>(result.Error);
    }

    [Fact]
    public async Task GetDoctors_UsesSnapshotNameForSpecialty()
    {
        Guid doctor = Guid.NewGuid();
        _repository.GetDoctorsBySpecialtyCodeAsync(_cardio, null).Returns(
            Result<IReadOnlyList<DoctorSummaryRow>, SpecialtyRepositoryError>.Success(
                [new() { UserCode = doctor, Names = "Ana", Surnames = "Ruiz", LicenseNumber = "CMP1" }]));

        var result = await _sut.GetDoctorsBySpecialtyCodeAsync(_cardio);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value);
        Assert.Equal(doctor, item.UserCode);
        Assert.Equal("Cardiología", item.SpecialtyName);
    }

    [Fact]
    public async Task GetDoctors_ForSpecialtyWithoutDoctors_ReturnsEmptyList()
    {
        _repository.GetDoctorsBySpecialtyCodeAsync(_pediatria, null).Returns(
            Result<IReadOnlyList<DoctorSummaryRow>, SpecialtyRepositoryError>.Success([]));

        var result = await _sut.GetDoctorsBySpecialtyCodeAsync(_pediatria);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetDoctors_ForUnknownDisabledOrInactiveSpecialty_ReturnsNotFound()
    {
        foreach (Guid code in new[] { Guid.NewGuid(), _disabled, _inactive })
        {
            var result = await _sut.GetDoctorsBySpecialtyCodeAsync(code);

            Assert.IsType<SpecialtyNotFoundError>(result.Error);
        }

        await _repository.DidNotReceiveWithAnyArgs().GetDoctorsBySpecialtyCodeAsync(default, default);
    }
}
