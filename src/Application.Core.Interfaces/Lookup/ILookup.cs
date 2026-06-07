using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using BindSharp;

namespace Application.Core.Interfaces.Lookup;

public interface ILookup
{
    Task<Result<MedicationLookupsResponse, LookupError>> GetMedicationLookupsAsync();
    Task<Result<UserLookupsResponse, LookupError>> GetUserLookupsAsync();
    Task<Result<PatientLookupsResponse, LookupError>> GetPatientLookupsAsync();
    Task<Result<PrescriptionLookupsResponse, LookupError>> GetPrescriptionLookupsAsync();
}
