using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using BindSharp;

namespace Application.Core.Interfaces.DocumentType;

public interface IDocumentType
{
    Task<Result<IEnumerable<GuidLookupItemResponse>, LookupError>> GetAllActiveAsync();
}
