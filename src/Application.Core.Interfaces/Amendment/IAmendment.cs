using Application.Core.DTOs.Amendment.Errors;
using Application.Core.DTOs.Amendment.Request;
using Application.Core.DTOs.Amendment.Response;
using BindSharp;

namespace Application.Core.Interfaces.Amendment;

/// <summary>Right to request an amendment of one's record (45 CFR 164.526). The record itself is never overwritten.</summary>
public interface IAmendment
{
    /// <summary>The patient submits a request about their own record.</summary>
    Task<Result<AmendmentResponse, AmendmentError>> CreateAsync(Guid patientCode, CreateAmendmentRequest request);

    /// <summary>The patient's own requests.</summary>
    Task<Result<AmendmentPageResponse, AmendmentError>> GetOwnPageAsync(
        Guid patientCode, AmendmentPageRequest request);

    /// <summary>Requests of one patient, for staff allowed to read that patient's clinical data.</summary>
    Task<Result<AmendmentPageResponse, AmendmentError>> GetPageByPatientAsync(
        Guid patientCode, AmendmentPageRequest request);

    /// <summary>Requests of every patient, for the clinician's review queue.</summary>
    Task<Result<AmendmentPageResponse, AmendmentError>> GetPageAsync(AmendmentPageRequest request);

    /// <summary>The clinician accepts (appending text to the record) or denies a pending request, always with a reason.</summary>
    Task<Result<AmendmentResponse, AmendmentError>> DecideAsync(
        Guid amendmentRequestCode, DecideAmendmentRequest request, Guid reviewerUserCode);
}
