using BindSharp;
using Infrastructure.Core.DTOs.Catalog;

namespace Infrastructure.Core.Interfaces.Catalog;

/// <summary>Which partner-side "enabled" table to consult.</summary>
public enum PartnerEnablementKind
{
    Medication,
    Specialty,
    Insurance
}

/// <summary>Reads the partner DB's enabled flags for catalog items (no row means not enabled).</summary>
public interface IPartnerEnablementRepository
{
    /// <summary>Returns the subset of <paramref name="codes"/> the partner has enabled.</summary>
    Task<Result<IReadOnlySet<Guid>, CatalogRepositoryError>> GetEnabledAsync(
        PartnerEnablementKind kind, IReadOnlyCollection<Guid> codes);
}
