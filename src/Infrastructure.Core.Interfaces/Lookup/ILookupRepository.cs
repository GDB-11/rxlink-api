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
    Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetAllergySeveritiesAsync();
    Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActivePrescriptionStatusesAsync();
    Task<Result<IEnumerable<MedicationLookupRow>, LookupRepositoryError>> GetActiveMedicationsAsync();
    Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActiveAdministrationRoutesAsync();
    Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActiveFrequenciesAsync();
    Task<Result<IEnumerable<GuidLookupRow>, LookupRepositoryError>> GetActiveConsultationTypesAsync();
    Task<Result<IEnumerable<InsuranceLookupRow>, LookupRepositoryError>> GetActiveInsurancesAsync();
    Task<Result<IEnumerable<SpecialtyPricingLookupRow>, LookupRepositoryError>> GetActiveSpecialtiesWithPricingAsync();
}