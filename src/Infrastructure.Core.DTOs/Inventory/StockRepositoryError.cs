namespace Infrastructure.Core.DTOs.Inventory;

public abstract record StockRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetStockError(string? Details = null, Exception? Exception = null)
    : StockRepositoryError("Error inesperado al recuperar el inventario.", Details, Exception);

public sealed record RegisterStockMovementError(string? Details = null, Exception? Exception = null)
    : StockRepositoryError("Error inesperado al registrar el movimiento de inventario.", Details, Exception);

/// <summary>A decrease would take the balance below zero (only a dispense override may do that).</summary>
public sealed record StockBelowZeroError(decimal Balance)
    : StockRepositoryError("El movimiento deja el stock por debajo de cero.");
