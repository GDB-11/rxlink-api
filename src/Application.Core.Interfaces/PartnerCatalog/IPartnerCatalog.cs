using Application.Core.DTOs.PartnerCatalog.Errors;
using Application.Core.DTOs.PartnerCatalog.Request;
using Application.Core.DTOs.PartnerCatalog.Response;
using BindSharp;

namespace Application.Core.Interfaces.PartnerCatalog;

/// <summary>What this partner enables, prices or overrides on top of the shared catalog.</summary>
public interface IPartnerCatalog
{
    Task<Result<PartnerMedicationResponse, PartnerCatalogError>> UpdateMedicationAsync(Guid code, UpdatePartnerMedicationRequest request);
    Task<Result<PartnerSpecialtyResponse, PartnerCatalogError>> UpdateSpecialtyAsync(Guid code, UpdatePartnerSpecialtyRequest request);
    Task<Result<PartnerInsuranceResponse, PartnerCatalogError>> UpdateInsuranceAsync(Guid code, UpdatePartnerInsuranceRequest request);
}
