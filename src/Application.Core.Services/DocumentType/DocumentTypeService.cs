using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using Application.Core.Interfaces.DocumentType;
using BindSharp;
using Infrastructure.Core.Interfaces.Catalog;

namespace Application.Core.Services.DocumentType;

public sealed class DocumentTypeService : IDocumentType
{
    private readonly ICatalogSnapshotStore _store;

    public DocumentTypeService(ICatalogSnapshotStore store)
    {
        _store = store;
    }

    /// <inheritdoc/>
    public Task<Result<IEnumerable<GuidLookupItemResponse>, LookupError>> GetAllActiveAsync() =>
        Task.FromResult(_store.Current is not { } catalog
            ? Result<IEnumerable<GuidLookupItemResponse>, LookupError>.Failure(new LookupCatalogUnavailableError())
            : Result<IEnumerable<GuidLookupItemResponse>, LookupError>.Success(
                catalog.DocumentTypes.Values.Where(d => d.IsActive).OrderBy(d => d.Name)
                    .Select(d => new GuidLookupItemResponse { Code = d.Code, Name = d.Name }).ToList()));
}
