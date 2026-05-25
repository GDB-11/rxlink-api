using Application.Core.DTOs.Allergy.Errors;
using Application.Core.DTOs.Allergy.Request;
using Application.Core.DTOs.Allergy.Response;
using BindSharp;

namespace Application.Core.Interfaces.Allergy;

public interface IAllergy
{
    /// <summary>Returns a paginated list of allergies. Supports optional text search on name.</summary>
    Task<Result<AllergyPageResponse, AllergyError>> GetPageAsync(AllergyPageRequest request);

    /// <summary>Registers a new allergy in the catalog.</summary>
    Task<Result<AllergyResponse, AllergyError>> CreateAsync(CreateAllergyRequest request);

    /// <summary>Updates an existing active allergy identified by its code.</summary>
    Task<Result<AllergyResponse, AllergyError>> UpdateAsync(Guid code, UpdateAllergyRequest request);

    /// <summary>Deactivates an allergy (soft-delete). The record is preserved to maintain FK integrity.</summary>
    Task<Result<Unit, AllergyError>> DeactivateAsync(Guid code, Guid performedByUserCode);

    /// <summary>Reactivates a previously deactivated allergy.</summary>
    Task<Result<Unit, AllergyError>> ActivateAsync(Guid code, Guid performedByUserCode);
}
