using Application.Core.DTOs.Disclosure.Errors;
using Application.Core.DTOs.Disclosure.Request;
using Application.Core.DTOs.Disclosure.Response;
using BindSharp;

namespace Application.Core.Interfaces.Disclosure;

/// <summary>Accounting of disclosures (45 CFR 164.528). Append-only: there is no update or delete.</summary>
public interface IDisclosure
{
    Task<Result<DisclosureResponse, DisclosureError>> RecordAsync(
        Guid patientCode, CreateDisclosureRequest request, Guid recordedByUserCode);

    /// <summary>Per-patient accounting report for staff.</summary>
    Task<Result<DisclosurePageResponse, DisclosureError>> GetPageAsync(
        Guid patientCode, DisclosurePageRequest request);

    /// <summary>The patient's own accounting of disclosures.</summary>
    Task<Result<DisclosurePageResponse, DisclosureError>> GetOwnPageAsync(
        Guid patientCode, DisclosurePageRequest request);
}
