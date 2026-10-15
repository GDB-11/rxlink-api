using BindSharp;
using Infrastructure.Core.DTOs.Lookup;
using Infrastructure.Core.Models.Lookup;

namespace Infrastructure.Core.Interfaces.Lookup;

/// <summary>Partner-owned lookup lists. Shared-catalog lists are served from the catalog snapshot, not here.</summary>
public interface ILookupRepository
{
    Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActiveRolesAsync();
    Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActivePrescriptionStatusesAsync();
    Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActiveConsultationTypesAsync();
}
