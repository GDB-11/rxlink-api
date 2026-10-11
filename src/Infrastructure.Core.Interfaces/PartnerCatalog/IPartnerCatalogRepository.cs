using BindSharp;
using Infrastructure.Core.DTOs.PartnerCatalog;
using Infrastructure.Core.Models.PartnerCatalog;

namespace Infrastructure.Core.Interfaces.PartnerCatalog;

/// <summary>The partner's own settings over shared-catalog items (<c>PartnerMedication/Specialty/Insurance</c>, <c>ClinicStock</c>).</summary>
public interface IPartnerCatalogRepository
{
    /// <summary>Current setting for a medication; a missing row reads as disabled (D27).</summary>
    Task<Result<PartnerMedicationRow, PartnerCatalogRepositoryError>> GetMedicationAsync(Guid code);

    Task<Result<PartnerSpecialtyRow, PartnerCatalogRepositoryError>> GetSpecialtyAsync(Guid code);

    Task<Result<PartnerInsuranceRow, PartnerCatalogRepositoryError>> GetInsuranceAsync(Guid code);

    /// <summary>Every medication setting row (with the default clinic's reorder level and stock). Items without a row are disabled.</summary>
    Task<Result<IReadOnlyList<PartnerMedicationRow>, PartnerCatalogRepositoryError>> GetAllMedicationsAsync();

    Task<Result<IReadOnlyList<PartnerSpecialtyRow>, PartnerCatalogRepositoryError>> GetAllSpecialtiesAsync();

    Task<Result<IReadOnlyList<PartnerInsuranceRow>, PartnerCatalogRepositoryError>> GetAllInsurancesAsync();

    /// <summary>Upserts the enabled flag and, when given, the reorder level of the default clinic's stock row.</summary>
    Task<Result<PartnerMedicationRow, PartnerCatalogRepositoryError>> SaveMedicationAsync(
        Guid code, bool isEnabled, decimal? reorderLevel);

    Task<Result<PartnerSpecialtyRow, PartnerCatalogRepositoryError>> SaveSpecialtyAsync(
        Guid code, bool isEnabled, decimal priceInPerson, decimal priceVirtual);

    Task<Result<PartnerInsuranceRow, PartnerCatalogRepositoryError>> SaveInsuranceAsync(
        Guid code, bool isEnabled, decimal? coveragePercentageOverride);
}
