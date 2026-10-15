using System.Collections.Frozen;
using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.Inventory.Errors;
using Application.Core.DTOs.Inventory.Request;
using Application.Core.Services.Inventory;
using BindSharp;
using Infrastructure.Core.DTOs.Inventory;
using Infrastructure.Core.DTOs.PartnerCatalog;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.Inventory;
using Infrastructure.Core.Interfaces.PartnerCatalog;
using Infrastructure.Core.Models.Catalog;
using Infrastructure.Core.Models.Inventory;
using Infrastructure.Core.Models.PartnerCatalog;
using NSubstitute;

namespace Application.Core.Services.Tests.Inventory;

public sealed class InventoryServiceTests
{
    private readonly ICatalogSnapshotStore _store = Substitute.For<ICatalogSnapshotStore>();
    private readonly IPartnerCatalogRepository _partner = Substitute.For<IPartnerCatalogRepository>();
    private readonly IStockRepository _stock = Substitute.For<IStockRepository>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly InventoryService _sut;

    private readonly Guid _out = Guid.NewGuid();
    private readonly Guid _low = Guid.NewGuid();
    private readonly Guid _ok = Guid.NewGuid();
    private readonly Guid _noLevel = Guid.NewGuid();
    private readonly Guid _disabled = Guid.NewGuid();

    public InventoryServiceTests()
    {
        _sut = new InventoryService(_store, _partner, _stock, _userContext);
        _store.Current.Returns(Snapshot());
    }

    private CatalogSnapshot Snapshot() => new()
    {
        Version = 1, SchemaVersion = 1, LoadedAt = DateTimeOffset.UtcNow,
        DocumentTypes = FrozenDictionary<Guid, DocumentTypeCatalogRow>.Empty,
        Sexes = FrozenDictionary<Guid, SexCatalogRow>.Empty,
        Specialties = FrozenDictionary<Guid, SpecialtyCatalogRow>.Empty,
        PharmaceuticalForms = FrozenDictionary<Guid, PharmaceuticalFormCatalogRow>.Empty,
        AdministrationRoutes = FrozenDictionary<Guid, AdministrationRouteCatalogRow>.Empty,
        Frequencies = FrozenDictionary<Guid, FrequencyCatalogRow>.Empty,
        Allergies = FrozenDictionary<Guid, AllergyCatalogRow>.Empty,
        AllergySeverities = FrozenDictionary<Guid, AllergySeverityCatalogRow>.Empty,
        Medications = new[] { _out, _low, _ok, _noLevel, _disabled }
            .ToDictionary(c => c, c => new MedicationCatalogRow
            {
                Code = c, PharmaceuticalFormCode = Guid.NewGuid(), AdministrationRouteCode = Guid.NewGuid(),
                GenericName = $"Med-{c.ToString()[..4]}", Concentration = "1 g", IsActive = true
            }).ToFrozenDictionary(),
        Insurances = FrozenDictionary<Guid, InsuranceCatalogRow>.Empty
    };

    private static Result<IReadOnlyList<PartnerMedicationRow>, PartnerCatalogRepositoryError> Ok(
        params PartnerMedicationRow[] rows) =>
        Result<IReadOnlyList<PartnerMedicationRow>, PartnerCatalogRepositoryError>.Success(rows);

    [Fact]
    public async Task LowStock_ListsOnlyEnabledItemsWithReorderLevelAtOrBelowIt_EmptiestFirst()
    {
        _partner.GetAllMedicationsAsync().Returns(Ok(
            new PartnerMedicationRow { MedicationCode = _low, IsEnabled = true, ReorderLevel = 10, StockQuantity = 4 },
            new PartnerMedicationRow { MedicationCode = _out, IsEnabled = true, ReorderLevel = 10, StockQuantity = null },
            new PartnerMedicationRow { MedicationCode = _ok, IsEnabled = true, ReorderLevel = 10, StockQuantity = 11 },
            new PartnerMedicationRow { MedicationCode = _noLevel, IsEnabled = true, StockQuantity = 0 },
            new PartnerMedicationRow { MedicationCode = _disabled, IsEnabled = false, ReorderLevel = 10, StockQuantity = 0 }));

        var result = await _sut.GetLowStockAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal([_out, _low], result.Value.Select(i => i.MedicationCode));
        Assert.Equal(["Out", "Low"], result.Value.Select(i => i.Status));
    }

    [Fact]
    public async Task LowStock_NoSnapshot_IsUnavailable()
    {
        _store.Current.Returns((CatalogSnapshot?)null);

        var result = await _sut.GetLowStockAsync();

        Assert.IsType<CatalogReadUnavailableError>(result.Error);
    }

    private readonly Guid _user = Guid.NewGuid();

    private void EnabledMedication(bool enabled = true) =>
        _partner.GetMedicationAsync(_ok).Returns(
            Result<PartnerMedicationRow, PartnerCatalogRepositoryError>.Success(
                new PartnerMedicationRow { MedicationCode = _ok, IsEnabled = enabled }));

    private static StockMovementRow Movement(string type, short direction, decimal quantity) => new()
    {
        StockMovementCode = Guid.NewGuid(), MovementType = type, Direction = direction, Quantity = quantity,
        OccurredAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Page_ListsOnlyEnabledItems_FiltersLowStockAndSearch()
    {
        _partner.GetAllMedicationsAsync().Returns(Ok(
            new PartnerMedicationRow { MedicationCode = _low, IsEnabled = true, ReorderLevel = 10, StockQuantity = 4 },
            new PartnerMedicationRow { MedicationCode = _ok, IsEnabled = true, ReorderLevel = 10, StockQuantity = 11 },
            new PartnerMedicationRow { MedicationCode = _disabled, IsEnabled = false, StockQuantity = 0 }));

        var all = await _sut.GetPageAsync(1, 20, null, lowStockOnly: false);
        var low = await _sut.GetPageAsync(1, 20, null, lowStockOnly: true);
        var none = await _sut.GetPageAsync(1, 20, "zzz", lowStockOnly: false);

        Assert.Equal(2, all.Value.TotalCount);
        Assert.Equal([_low], low.Value.Items.Select(i => i.MedicationCode));
        Assert.Equal(0, none.Value.TotalCount);
    }

    [Fact]
    public async Task Register_Entry_PassesPositiveDirectionAndTrimmedValues()
    {
        _userContext.UserCode.Returns(_user);
        EnabledMedication();
        _stock.RegisterMovementAsync(_ok, "Entry", 1, 5m, "L1", null, null, _user)
            .Returns(Result<StockMovementRow, StockRepositoryError>.Success(Movement("Entry", 1, 5m)));

        var result = await _sut.RegisterMovementAsync(_ok,
            new RegisterStockMovementRequest { MovementType = "Entry", Quantity = 5m, BatchNumber = " L1 " });

        Assert.True(result.IsSuccess);
        Assert.Equal("Entry", result.Value.MovementType);
    }

    [Theory]
    [InlineData("Dispense", null, 1, null)]      // not a manual type
    [InlineData("Entry", null, 0, null)]         // zero quantity
    [InlineData("Entry", null, 1.2345, null)]    // too many decimals
    [InlineData("Adjustment", null, 1, "motivo")] // adjustment without direction
    [InlineData("Adjustment", 1, 1, null)]       // adjustment without reason
    [InlineData("Loss", null, 1, " ")]           // loss without reason
    [InlineData("Loss", 1, 1, "vencido")]        // direction only for adjustments
    public async Task Register_BrokenRule_IsInvalidRequest(string type, int? direction, double quantity, string? reason)
    {
        _userContext.UserCode.Returns(_user);
        EnabledMedication();

        var result = await _sut.RegisterMovementAsync(_ok, new RegisterStockMovementRequest
        {
            MovementType = type, Direction = direction, Quantity = (decimal)quantity, Reason = reason
        });

        Assert.IsType<InventoryInvalidRequestError>(result.Error);
        await _stock.DidNotReceiveWithAnyArgs().RegisterMovementAsync(default, default!, default, default, default, default, default, default);
    }

    [Fact]
    public async Task Register_DisabledMedication_IsNotFound()
    {
        _userContext.UserCode.Returns(_user);
        EnabledMedication(enabled: false);

        var result = await _sut.RegisterMovementAsync(_ok, new RegisterStockMovementRequest { MovementType = "Entry", Quantity = 1 });

        Assert.IsType<InventoryMedicationNotFoundError>(result.Error);
    }

    [Fact]
    public async Task Register_DecreaseAboveBalance_IsInsufficientStock()
    {
        _userContext.UserCode.Returns(_user);
        EnabledMedication();
        _stock.RegisterMovementAsync(_ok, "Loss", -1, 9m, null, null, "vencido", _user)
            .Returns(Result<StockMovementRow, StockRepositoryError>.Failure(new StockBelowZeroError(3m)));

        var result = await _sut.RegisterMovementAsync(_ok,
            new RegisterStockMovementRequest { MovementType = "Loss", Quantity = 9m, Reason = "vencido" });

        var error = Assert.IsType<InventoryInsufficientStockError>(result.Error);
        Assert.Equal(3m, error.Balance);
    }

    [Fact]
    public async Task Report_InvertedRange_IsInvalid()
    {
        var result = await _sut.GetReportAsync(new StockReportRequest
        {
            From = new DateOnly(2026, 10, 10), To = new DateOnly(2026, 10, 1)
        });

        Assert.IsType<InventoryInvalidRequestError>(result.Error);
    }
}
