using BindSharp;
using Infrastructure.Core.DTOs.Catalog;

namespace Infrastructure.Core.Interfaces.Catalog;

/// <summary>Reads every row of one catalog table from the catalog database.</summary>
public interface ICatalogReader<TRow>
{
    Task<Result<IReadOnlyList<TRow>, CatalogRepositoryError>> GetAllAsync();
}
