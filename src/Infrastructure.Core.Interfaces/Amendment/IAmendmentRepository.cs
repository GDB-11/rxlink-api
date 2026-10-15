using BindSharp;
using Infrastructure.Core.DTOs.Amendment;
using Infrastructure.Core.Models.Amendment;

namespace Infrastructure.Core.Interfaces.Amendment;

public interface IAmendmentRepository
{
    /// <summary>Inserts a pending request. <c>null</c> when no active patient has that code.</summary>
    Task<Result<AmendmentRow?, AmendmentRepositoryError>> InsertAsync(
        Guid patientCode, string targetArea, string requestedChange);

    Task<Result<AmendmentRow?, AmendmentRepositoryError>> GetByCodeAsync(Guid amendmentRequestCode);

    /// <summary>Requests of one patient, newest first.</summary>
    Task<Result<IEnumerable<AmendmentRow>, AmendmentRepositoryError>> GetPageByPatientAsync(
        Guid patientCode, int offset, int limit);

    /// <summary>Requests of every patient, optionally filtered by status; pending ones oldest first.</summary>
    Task<Result<IEnumerable<AmendmentRow>, AmendmentRepositoryError>> GetPageAsync(
        string? status, int offset, int limit);

    /// <summary>
    /// Accepts or denies a request that is still pending. <c>null</c> when it is not pending any more
    /// (someone decided it first) or does not exist.
    /// </summary>
    Task<Result<AmendmentRow?, AmendmentRepositoryError>> DecideAsync(
        Guid amendmentRequestCode, bool accepted, string reason, string? amendmentText, Guid reviewerUserCode);
}
