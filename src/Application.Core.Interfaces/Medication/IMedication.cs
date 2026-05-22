using Application.Core.DTOs.Medication.Errors;
using Application.Core.DTOs.Medication.Request;
using Application.Core.DTOs.Medication.Response;
using BindSharp;

namespace Application.Core.Interfaces.Medication;

public interface IMedication
{
    Task<Result<MedicationPageResponse, MedicationError>> GetPageAsync(MedicationPageRequest request);
    Task<Result<MedicationResponse, MedicationError>>     CreateAsync(CreateMedicationRequest request);
    Task<Result<MedicationResponse, MedicationError>>     UpdateAsync(Guid code, UpdateMedicationRequest request);
    Task<Result<Unit, MedicationError>>                   DeactivateAsync(Guid code, Guid performedByUserCode);
}
