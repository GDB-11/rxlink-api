using BindSharp;
using Infrastructure.Core.DTOs.Catalog;
using Infrastructure.Core.Models.Catalog;

namespace Infrastructure.Core.Interfaces.Catalog;

/// <summary>Reads this deployment's own registry identity and logo through the catalog connection's self-only views.</summary>
public interface IPartnerRegistryReader
{
    /// <summary>Returns the identity row, or <c>null</c> when the catalog role is not registered to a partner.</summary>
    Task<Result<PartnerIdentityRow?, CatalogRepositoryError>> GetIdentityAsync();

    /// <summary>Returns the logo, or <c>null</c> when none has been uploaded.</summary>
    Task<Result<PartnerLogoRow?, CatalogRepositoryError>> GetLogoAsync();
}
