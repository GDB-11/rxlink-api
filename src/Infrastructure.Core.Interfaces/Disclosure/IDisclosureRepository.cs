using BindSharp;
using Infrastructure.Core.DTOs.Disclosure;
using Infrastructure.Core.Models.Disclosure;

namespace Infrastructure.Core.Interfaces.Disclosure;

public interface IDisclosureRepository
{
    /// <summary>Appends a disclosure. <c>null</c> when no active patient has that code.</summary>
    Task<Result<DisclosureRow?, DisclosureRepositoryError>> InsertAsync(
        Guid patientCode, DateTimeOffset disclosedAt, string recipientName, string recipientType,
        string purpose, string description, Guid recordedByUserCode);

    /// <summary>Disclosures of one patient, newest first.</summary>
    Task<Result<IEnumerable<DisclosureRow>, DisclosureRepositoryError>> GetPageAsync(
        Guid patientCode, int offset, int limit);
}
