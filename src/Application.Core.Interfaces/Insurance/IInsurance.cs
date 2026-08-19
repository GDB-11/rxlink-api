using Application.Core.DTOs.Insurance.Errors;
using Application.Core.DTOs.Insurance.Request;
using Application.Core.DTOs.Insurance.Response;
using BindSharp;

namespace Application.Core.Interfaces.Insurance;

public interface IInsurance
{
    Task<Result<InsurancePageResponse, InsuranceError>> GetPageAsync(InsurancePageRequest request);
    Task<Result<InsuranceResponse, InsuranceError>> CreateAsync(CreateInsuranceRequest request);
    Task<Result<InsuranceResponse, InsuranceError>> UpdateAsync(Guid code, UpdateInsuranceRequest request);
    Task<Result<Unit, InsuranceError>> DeactivateAsync(Guid code);
    Task<Result<Unit, InsuranceError>> ActivateAsync(Guid code);
}
