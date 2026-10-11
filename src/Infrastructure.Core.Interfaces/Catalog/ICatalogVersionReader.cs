using BindSharp;
using Infrastructure.Core.DTOs.Catalog;
using Infrastructure.Core.Models.Catalog;

namespace Infrastructure.Core.Interfaces.Catalog;

/// <summary>Reads the single <c>CatalogVersion</c> row, the cheap "did anything change" signal.</summary>
public interface ICatalogVersionReader
{
    Task<Result<CatalogVersionRow, CatalogRepositoryError>> GetAsync();
}
