using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using BindSharp;

namespace Application.Core.Interfaces.Sex;

public interface ISex
{
    Task<Result<IEnumerable<GuidLookupItemResponse>, LookupError>> GetAllAsync();
}