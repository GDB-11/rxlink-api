using BindSharp;
using Infrastructure.Core.DTOs.Lookup;
using Infrastructure.Core.Models.Lookup;

namespace Infrastructure.Core.Interfaces.Lookup;

public interface ILookupRepository
{
    Task<Result<IEnumerable<LookupRow>, LookupRepositoryError>> GetPharmaceuticalFormsAsync();
    Task<Result<IEnumerable<LookupRow>, LookupRepositoryError>> GetAdministrationRoutesAsync();
    Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetSexesAsync();
    Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActiveDocumentTypesAsync();
    Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActiveRolesAsync();
    Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActiveSpecialtiesAsync();
}
