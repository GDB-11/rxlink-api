using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using Application.Core.Interfaces.DocumentType;
using BindSharp;
using Infrastructure.Core.Interfaces.Lookup;
using Infrastructure.Core.Models.Lookup;

namespace Application.Core.Services.DocumentType;

public sealed class DocumentTypeService : IDocumentType
{
    private readonly ILookupRepository _repository;

    public DocumentTypeService(ILookupRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task<Result<IEnumerable<GuidLookupItemResponse>, LookupError>> GetAllActiveAsync() =>
        _repository.GetActiveDocumentTypesAsync()
            .MapErrorAsync(LookupError (e) => new LookupDataAccessError(e.Message, e.Details, e.Exception))
            .MapAsync(rows => rows.Select(ToResponse));

    private static GuidLookupItemResponse ToResponse(GuidLookupRow row) =>
        new() { Code = row.Code, Name = row.Name };
}
