using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using BindSharp;

namespace Application.Core.Interfaces.ConsultationType;

public interface IConsultationType
{
    Task<Result<IEnumerable<GuidLookupItemResponse>, LookupError>> GetAllActiveAsync();
}