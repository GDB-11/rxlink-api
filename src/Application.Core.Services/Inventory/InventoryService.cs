using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.Catalog.Response;
using Application.Core.DTOs.Inventory.Errors;
using Application.Core.DTOs.Inventory.Request;
using Application.Core.DTOs.Inventory.Response;
using Application.Core.Interfaces.Inventory;
using BindSharp;
using Infrastructure.Core.DTOs.Inventory;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.Inventory;
using Infrastructure.Core.Interfaces.PartnerCatalog;
using Infrastructure.Core.Models.Catalog;
using Infrastructure.Core.Models.Inventory;
using Infrastructure.Core.Models.PartnerCatalog;

namespace Application.Core.Services.Inventory;

/// <summary>Stock: partner balances joined with medication names from the catalog snapshot, plus the ledger.</summary>
public sealed class InventoryService : IInventory
{
    private const int MaxRangeDays = 366;
    private const int MaxPageSize = 100;

    private static readonly string[] ManualTypes = ["Entry", "Adjustment", "Loss"];

    private readonly ICatalogSnapshotStore _store;
    private readonly IPartnerCatalogRepository _partner;
    private readonly IStockRepository _stock;
    private readonly IUserContext _userContext;

    public InventoryService(
        ICatalogSnapshotStore store, IPartnerCatalogRepository partner, IStockRepository stock, IUserContext userContext)
    {
        _store = store;
        _partner = partner;
        _stock = stock;
        _userContext = userContext;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<LowStockItemResponse>, CatalogReadError>> GetLowStockAsync()
    {
        if (_store.Current is not { } catalog)
            return Result<IReadOnlyList<LowStockItemResponse>, CatalogReadError>.Failure(new CatalogReadUnavailableError());

        var settings = await _partner.GetAllMedicationsAsync();
        if (!settings.IsSuccess)
            return Result<IReadOnlyList<LowStockItemResponse>, CatalogReadError>.Failure(
                new CatalogReadDataAccessError(settings.Error.Message, settings.Error.Details, settings.Error.Exception));

        IReadOnlyList<LowStockItemResponse> items =
        [
            .. settings.Value
                .Where(m => m is { IsEnabled: true, ReorderLevel: not null }
                            && StockStatus.From(m.StockQuantity, m.ReorderLevel) != StockStatus.InStock
                            && catalog.Medications.ContainsKey(m.MedicationCode))
                .Select(m => new LowStockItemResponse
                {
                    MedicationCode = m.MedicationCode,
                    Name = MedicationName(catalog.Medications[m.MedicationCode]),
                    StockQuantity = m.StockQuantity ?? 0,
                    ReorderLevel = m.ReorderLevel!.Value,
                    Status = StockStatus.From(m.StockQuantity, m.ReorderLevel)
                })
                .OrderBy(i => i.StockQuantity)
                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
        ];
        return Result<IReadOnlyList<LowStockItemResponse>, CatalogReadError>.Success(items);
    }

    /// <inheritdoc/>
    public async Task<Result<CatalogPageResponse<InventoryItemResponse>, InventoryError>> GetPageAsync(
        int page, int pageSize, string? search, bool lowStockOnly)
    {
        if (_store.Current is not { } catalog)
            return Fail<CatalogPageResponse<InventoryItemResponse>>(new InventoryUnavailableError());

        var settings = await _partner.GetAllMedicationsAsync();
        if (!settings.IsSuccess)
            return Fail<CatalogPageResponse<InventoryItemResponse>>(
                new InventoryDataAccessError(settings.Error.Message, settings.Error.Details, settings.Error.Exception));

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        string? term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        List<InventoryItemResponse> matches = settings.Value
            .Where(m => m.IsEnabled && catalog.Medications.ContainsKey(m.MedicationCode))
            .Select(m => ToItem(m, catalog.Medications[m.MedicationCode], catalog))
            .Where(i => !lowStockOnly || i.Status != StockStatus.InStock)
            .Where(i => term is null
                        || i.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int totalPages = (int)Math.Ceiling(matches.Count / (double)pageSize);
        return Result<CatalogPageResponse<InventoryItemResponse>, InventoryError>.Success(
            new CatalogPageResponse<InventoryItemResponse>
            {
                Items = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                TotalCount = matches.Count,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
    }

    /// <inheritdoc/>
    public async Task<Result<CatalogPageResponse<StockMovementResponse>, InventoryError>> GetMovementsAsync(
        Guid medicationCode, int page, int pageSize)
    {
        var check = await EnsureEnabledAsync(medicationCode);
        if (!check.IsSuccess)
            return Fail<CatalogPageResponse<StockMovementResponse>>(check.Error);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var result = await _stock.GetMovementsAsync(medicationCode, page, pageSize);
        if (!result.IsSuccess)
            return Fail<CatalogPageResponse<StockMovementResponse>>(DataAccess(result.Error));

        return Result<CatalogPageResponse<StockMovementResponse>, InventoryError>.Success(
            new CatalogPageResponse<StockMovementResponse>
            {
                Items = result.Value.Items.Select(ToResponse).ToList(),
                TotalCount = result.Value.TotalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(result.Value.TotalCount / (double)pageSize)
            });
    }

    /// <inheritdoc/>
    public async Task<Result<StockMovementResponse, InventoryError>> RegisterMovementAsync(
        Guid medicationCode, RegisterStockMovementRequest request)
    {
        if (_userContext.UserCode is not { } userCode)
            return Fail<StockMovementResponse>(new InventoryInvalidRequestError("No se pudo identificar al usuario."));

        string? invalid = Validate(request, out short direction);
        if (invalid is not null)
            return Fail<StockMovementResponse>(new InventoryInvalidRequestError(invalid));

        var check = await EnsureEnabledAsync(medicationCode);
        if (!check.IsSuccess)
            return Fail<StockMovementResponse>(check.Error);

        var result = await _stock.RegisterMovementAsync(
            medicationCode, request.MovementType, direction, request.Quantity,
            Normalize(request.BatchNumber), request.ExpiresOn?.ToDateTime(TimeOnly.MinValue),
            Normalize(request.Reason), userCode);

        return result.IsSuccess
            ? Result<StockMovementResponse, InventoryError>.Success(ToResponse(result.Value))
            : Fail<StockMovementResponse>(result.Error is StockBelowZeroError below
                ? new InventoryInsufficientStockError(below.Balance)
                : DataAccess(result.Error));
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<StockReportItemResponse>, InventoryError>> GetReportAsync(
        StockReportRequest request)
    {
        if (request.From > request.To || request.To.DayNumber - request.From.DayNumber >= MaxRangeDays)
            return Fail<IReadOnlyList<StockReportItemResponse>>(
                new InventoryInvalidRequestError("El rango de fechas no es válido (máximo 366 días)."));

        if (_store.Current is not { } catalog)
            return Fail<IReadOnlyList<StockReportItemResponse>>(new InventoryUnavailableError());

        var balances = await _partner.GetAllMedicationsAsync();
        if (!balances.IsSuccess)
            return Fail<IReadOnlyList<StockReportItemResponse>>(
                new InventoryDataAccessError(balances.Error.Message, balances.Error.Details, balances.Error.Exception));

        var totals = await _stock.GetReportAsync(
            new DateTimeOffset(request.From.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            new DateTimeOffset(request.To.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (!totals.IsSuccess)
            return Fail<IReadOnlyList<StockReportItemResponse>>(DataAccess(totals.Error));

        Dictionary<Guid, PartnerMedicationRow> current = balances.Value.ToDictionary(m => m.MedicationCode);

        IReadOnlyList<StockReportItemResponse> items =
        [
            .. totals.Value
                .Where(t => catalog.Medications.ContainsKey(t.MedicationCode))
                .Select(t => new StockReportItemResponse
                {
                    MedicationCode = t.MedicationCode,
                    Name = MedicationName(catalog.Medications[t.MedicationCode]),
                    Entries = t.Entries,
                    Returns = t.Returns,
                    Dispensed = t.Dispensed,
                    Losses = t.Losses,
                    Adjustments = t.Adjustments,
                    Shortfall = t.Shortfall,
                    StockQuantity = current.TryGetValue(t.MedicationCode, out var m) ? m.StockQuantity ?? 0 : 0
                })
                .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
        ];
        return Result<IReadOnlyList<StockReportItemResponse>, InventoryError>.Success(items);
    }

    /// <summary>Returns the first broken rule, or <c>null</c>; <paramref name="direction"/> is the signed effect on the balance.</summary>
    private static string? Validate(RegisterStockMovementRequest request, out short direction)
    {
        direction = 0;
        if (!ManualTypes.Contains(request.MovementType))
            return "El tipo de movimiento debe ser Entry, Adjustment o Loss.";
        if (request.Quantity <= 0 || request.Quantity > 999_999_999m || decimal.Round(request.Quantity, 3) != request.Quantity)
            return "La cantidad debe ser mayor que cero y tener como máximo 3 decimales.";

        direction = request.MovementType switch
        {
            "Entry" => 1,
            "Loss" => -1,
            _ => request.Direction switch { 1 => 1, -1 => -1, _ => 0 }
        };
        if (direction == 0)
            return "Un ajuste debe indicar si aumenta (1) o disminuye (-1) el stock.";
        if (request.MovementType != "Adjustment" && request.Direction is not null)
            return "La dirección solo aplica a los ajustes.";
        if (request.MovementType != "Entry" && (request.BatchNumber is not null || request.ExpiresOn is not null))
            return "El lote y la fecha de vencimiento solo aplican a las entradas.";
        if (request.MovementType != "Entry" && string.IsNullOrWhiteSpace(request.Reason))
            return "El motivo es obligatorio para ajustes y pérdidas.";
        return null;
    }

    private async Task<Result<bool, InventoryError>> EnsureEnabledAsync(Guid medicationCode)
    {
        if (_store.Current is not { } catalog)
            return Result<bool, InventoryError>.Failure(new InventoryUnavailableError());

        var setting = await _partner.GetMedicationAsync(medicationCode);
        if (!setting.IsSuccess)
            return Result<bool, InventoryError>.Failure(
                new InventoryDataAccessError(setting.Error.Message, setting.Error.Details, setting.Error.Exception));

        return catalog.Medications.ContainsKey(medicationCode) && setting.Value.IsEnabled
            ? Result<bool, InventoryError>.Success(true)
            : Result<bool, InventoryError>.Failure(new InventoryMedicationNotFoundError());
    }

    private static Result<T, InventoryError> Fail<T>(InventoryError error) => Result<T, InventoryError>.Failure(error);

    private static InventoryError DataAccess(StockRepositoryError error) =>
        new InventoryDataAccessError(error.Message, error.Details, error.Exception);

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static InventoryItemResponse ToItem(PartnerMedicationRow m, MedicationCatalogRow row, CatalogSnapshot catalog) =>
        new()
        {
            MedicationCode = m.MedicationCode,
            Name = MedicationName(row),
            Concentration = row.Concentration,
            PharmaceuticalFormName = catalog.PharmaceuticalForms.TryGetValue(row.PharmaceuticalFormCode, out var form)
                ? form.Name
                : string.Empty,
            StockQuantity = m.StockQuantity ?? 0,
            ReorderLevel = m.ReorderLevel,
            Status = StockStatus.From(m.StockQuantity, m.ReorderLevel)
        };

    private static StockMovementResponse ToResponse(StockMovementRow row) =>
        new()
        {
            StockMovementCode = row.StockMovementCode,
            MovementType = row.MovementType,
            Direction = row.Direction,
            Quantity = row.Quantity,
            ShortfallQuantity = row.ShortfallQuantity,
            BatchNumber = row.BatchNumber,
            ExpiresOn = row.ExpiresOn is { } d ? DateOnly.FromDateTime(d) : null,
            Reason = row.Reason,
            Username = row.Username,
            OccurredAt = row.OccurredAt
        };

    private static string MedicationName(MedicationCatalogRow m) =>
        m.CommercialName is null ? m.GenericName : $"{m.CommercialName} - {m.GenericName}";
}
