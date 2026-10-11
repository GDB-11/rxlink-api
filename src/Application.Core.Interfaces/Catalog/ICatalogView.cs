using Application.Core.DTOs.Allergy.Request;
using Application.Core.DTOs.Allergy.Response;
using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.Catalog.Response;
using Application.Core.DTOs.Insurance.Request;
using Application.Core.DTOs.Insurance.Response;
using Application.Core.DTOs.Medication.Request;
using Application.Core.DTOs.Medication.Response;
using Application.Core.DTOs.Specialty.Request;
using Application.Core.DTOs.Specialty.Response;
using BindSharp;

namespace Application.Core.Interfaces.Catalog;

/// <summary>Read side of the shared catalog: snapshot items merged with this partner's own settings.</summary>
public interface ICatalogView
{
    Task<Result<CatalogPageResponse<MedicationCatalogItemResponse>, CatalogReadError>> GetMedicationsPageAsync(
        MedicationPageRequest request);

    Task<Result<CatalogPageResponse<SpecialtyCatalogItemResponse>, CatalogReadError>> GetSpecialtiesPageAsync(
        SpecialtyPageRequest request);

    Task<Result<CatalogPageResponse<InsuranceCatalogItemResponse>, CatalogReadError>> GetInsurancesPageAsync(
        InsurancePageRequest request);

    Task<Result<CatalogPageResponse<AllergyResponse>, CatalogReadError>> GetAllergiesPageAsync(
        AllergyPageRequest request);
}
