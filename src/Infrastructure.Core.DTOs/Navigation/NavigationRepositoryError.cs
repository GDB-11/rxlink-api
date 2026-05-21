namespace Infrastructure.Core.DTOs.Navigation;

/// <summary>Base type for all navigation-repository errors.</summary>
public abstract record NavigationRepositoryError(string Message, string? Details = null, Exception? Exception = null);

public sealed record GetNavigationRowsError(string? Details = null, Exception? Exception = null)
    : NavigationRepositoryError("Error inesperado al recuperar la navegación desde la base de datos.", Details, Exception);