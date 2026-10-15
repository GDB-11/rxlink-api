namespace Application.Core.DTOs.Inventory.Errors;

/// <summary>Why an inventory read or stock movement was rejected.</summary>
public abstract record InventoryError(string Message, string? Details = null, Exception? Exception = null);

/// <summary>No catalog snapshot has loaded, so medications cannot be resolved.</summary>
public sealed record InventoryUnavailableError()
    : InventoryError("El catálogo compartido no está disponible en este momento. Intente nuevamente más tarde.");

/// <summary>The medication is not in the catalog or this partner has not enabled it.</summary>
public sealed record InventoryMedicationNotFoundError()
    : InventoryError("El medicamento no existe o no está habilitado en este centro.");

/// <summary>The request breaks a rule (type, quantity, reason, range).</summary>
public sealed record InventoryInvalidRequestError(string Message) : InventoryError(Message);

/// <summary>A decrease exceeds the current balance.</summary>
public sealed record InventoryInsufficientStockError(decimal Balance)
    : InventoryError($"El stock actual ({Balance:0.###}) no cubre la cantidad indicada.");

public sealed record InventoryDataAccessError(string Message, string? Details = null, Exception? Exception = null)
    : InventoryError(Message, Details, Exception);
