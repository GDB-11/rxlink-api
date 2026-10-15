using BindSharp;
using Infrastructure.Core.DTOs.Catalog;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Models.Catalog;
using Infrastructure.Core.Services.Catalog;
using NSubstitute;

namespace Application.Core.Services.Tests.Catalog;

public sealed class CatalogSnapshotStoreTests
{
    private readonly ICatalogVersionReader _version = Substitute.For<ICatalogVersionReader>();
    private readonly ICatalogReader<DocumentTypeCatalogRow> _documentTypes = Reader<DocumentTypeCatalogRow>();
    private readonly ICatalogReader<MedicationCatalogRow> _medications = Reader<MedicationCatalogRow>();
    private readonly ICatalogReader<MedicationClassCatalogRow> _classes = Reader<MedicationClassCatalogRow>();
    private readonly ICatalogReader<MedicationClassMemberCatalogRow> _members = Reader<MedicationClassMemberCatalogRow>();
    private readonly ICatalogReader<AllergyMedicationClassCatalogRow> _allergyClasses = Reader<AllergyMedicationClassCatalogRow>();
    private readonly ICatalogReader<MedicationInteractionCatalogRow> _interactions = Reader<MedicationInteractionCatalogRow>();
    private readonly CatalogSnapshotStore _sut;

    public CatalogSnapshotStoreTests()
    {
        _version.GetAsync().Returns(Result<CatalogVersionRow, CatalogRepositoryError>.Success(
            new CatalogVersionRow { Version = 7, SchemaVersion = 1 }));

        _sut = new CatalogSnapshotStore(
            _version,
            _documentTypes,
            Reader<SexCatalogRow>(),
            Reader<SpecialtyCatalogRow>(),
            Reader<PharmaceuticalFormCatalogRow>(),
            Reader<AdministrationRouteCatalogRow>(),
            Reader<FrequencyCatalogRow>(),
            Reader<AllergyCatalogRow>(),
            Reader<AllergySeverityCatalogRow>(),
            _medications,
            Reader<InsuranceCatalogRow>(),
            _classes,
            _members,
            _allergyClasses,
            _interactions,
            TimeProvider.System);
    }

    private static ICatalogReader<T> Reader<T>(params T[] rows)
    {
        ICatalogReader<T> reader = Substitute.For<ICatalogReader<T>>();
        reader.GetAllAsync().Returns(Result<IReadOnlyList<T>, CatalogRepositoryError>.Success(rows));
        return reader;
    }

    [Fact]
    public async Task RefreshAsync_BuildsSnapshotIndexedByCode_IncludingInactiveRows()
    {
        Guid active = Guid.NewGuid(), inactive = Guid.NewGuid();
        _documentTypes.GetAllAsync().Returns(Result<IReadOnlyList<DocumentTypeCatalogRow>, CatalogRepositoryError>
            .Success([
                new DocumentTypeCatalogRow { Code = active, Name = "DNI", IsActive = true },
                new DocumentTypeCatalogRow { Code = inactive, Name = "Old", IsActive = false }
            ]));

        var result = await _sut.RefreshAsync();

        Assert.True(result.IsSuccess);
        Assert.Same(result.Value, _sut.Current);
        Assert.Equal(7, result.Value.Version);
        Assert.Equal("DNI", result.Value.DocumentTypes[active].Name);
        Assert.False(result.Value.DocumentTypes[inactive].IsActive);
    }

    [Fact]
    public async Task RefreshAsync_BeforeFirstLoad_CurrentIsNull() => Assert.Null(_sut.Current);

    [Fact]
    public async Task RefreshAsync_WhenATableFails_KeepsThePreviousSnapshot()
    {
        var first = await _sut.RefreshAsync();
        _medications.GetAllAsync().Returns(Result<IReadOnlyList<MedicationCatalogRow>, CatalogRepositoryError>
            .Failure(new CatalogLoadError("boom")));

        var second = await _sut.RefreshAsync();

        Assert.True(second.IsFailure);
        Assert.Same(first.Value, _sut.Current);
    }

    [Fact]
    public async Task RefreshAsync_WhenVersionCannotBeRead_LeavesCurrentNull()
    {
        _version.GetAsync().Returns(Result<CatalogVersionRow, CatalogRepositoryError>
            .Failure(new CatalogLoadError("down")));

        var result = await _sut.RefreshAsync();

        Assert.True(result.IsFailure);
        Assert.Null(_sut.Current);
    }

    [Fact]
    public async Task RefreshIfChangedAsync_WithoutSnapshot_LoadsIt()
    {
        var result = await _sut.RefreshIfChangedAsync();

        Assert.True(result.IsSuccess);
        Assert.Same(result.Value, _sut.Current);
    }

    [Fact]
    public async Task RefreshIfChangedAsync_WhenVersionUnchanged_DoesNotReloadTables()
    {
        var first = await _sut.RefreshAsync();
        _medications.ClearReceivedCalls();

        var second = await _sut.RefreshIfChangedAsync();

        Assert.Same(first.Value, second.Value);
        await _medications.DidNotReceive().GetAllAsync();
    }

    [Fact]
    public async Task RefreshIfChangedAsync_WhenVersionChanged_SwapsInANewSnapshot()
    {
        var first = await _sut.RefreshAsync();
        _version.GetAsync().Returns(Result<CatalogVersionRow, CatalogRepositoryError>.Success(
            new CatalogVersionRow { Version = 8, SchemaVersion = 1 }));

        var second = await _sut.RefreshIfChangedAsync();

        Assert.Equal(8, second.Value.Version);
        Assert.NotSame(first.Value, _sut.Current);
    }

    [Fact]
    public async Task RefreshIfChangedAsync_WhenCatalogGoesDown_KeepsSnapshotAndRecordsFailureUntilRecovery()
    {
        var first = await _sut.RefreshAsync();
        Assert.Null(_sut.LastFailureAt);

        _version.GetAsync().Returns(Result<CatalogVersionRow, CatalogRepositoryError>
            .Failure(new CatalogLoadError("down")));
        var failed = await _sut.RefreshIfChangedAsync();

        Assert.True(failed.IsFailure);
        Assert.Same(first.Value, _sut.Current);
        Assert.NotNull(_sut.LastFailureAt);

        _version.GetAsync().Returns(Result<CatalogVersionRow, CatalogRepositoryError>.Success(
            new CatalogVersionRow { Version = 7, SchemaVersion = 1 }));
        var recovered = await _sut.RefreshIfChangedAsync();

        Assert.True(recovered.IsSuccess);
        Assert.Null(_sut.LastFailureAt);
    }

    [Fact]
    public void GetRequired_WithoutSnapshot_ReturnsCatalogUnavailable()
    {
        var result = _sut.GetRequired();

        Assert.IsType<CatalogUnavailableError>(result.Error);
    }

    [Fact]
    public async Task RefreshAsync_IndexesRulesByCode_DroppingInactiveClassesAndTheirLinks()
    {
        Guid med = Guid.NewGuid(), allergy = Guid.NewGuid();
        Guid active = Guid.NewGuid(), inactive = Guid.NewGuid();
        _classes.GetAllAsync().Returns(Result<IReadOnlyList<MedicationClassCatalogRow>, CatalogRepositoryError>
            .Success([
                new MedicationClassCatalogRow { Code = active, Name = "Penicilinas", IsActive = true },
                new MedicationClassCatalogRow { Code = inactive, Name = "Vieja", IsActive = false }
            ]));
        _members.GetAllAsync().Returns(Result<IReadOnlyList<MedicationClassMemberCatalogRow>, CatalogRepositoryError>
            .Success([
                new MedicationClassMemberCatalogRow { ClassCode = active, MedicationCode = med },
                new MedicationClassMemberCatalogRow { ClassCode = inactive, MedicationCode = med }
            ]));
        _allergyClasses.GetAllAsync().Returns(
            Result<IReadOnlyList<AllergyMedicationClassCatalogRow>, CatalogRepositoryError>.Success([
                new AllergyMedicationClassCatalogRow { AllergyCode = allergy, ClassCode = active },
                new AllergyMedicationClassCatalogRow { AllergyCode = allergy, ClassCode = inactive }
            ]));
        _interactions.GetAllAsync().Returns(
            Result<IReadOnlyList<MedicationInteractionCatalogRow>, CatalogRepositoryError>.Success([
                new MedicationInteractionCatalogRow
                {
                    Code = Guid.NewGuid(), ClassACode = active, ClassBCode = active, Severity = "Major",
                    Description = "x"
                },
                new MedicationInteractionCatalogRow
                {
                    Code = Guid.NewGuid(), ClassACode = active, ClassBCode = inactive, Severity = "Major",
                    Description = "y"
                }
            ]));

        var snapshot = (await _sut.RefreshAsync()).Value;

        Assert.Equal([active], snapshot.ClassesByMedication[med]);
        Assert.Equal([active], snapshot.ClassesByAllergy[allergy]);
        Assert.Single(snapshot.MedicationInteractions);
    }
}
