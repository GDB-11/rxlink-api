using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using Application.Core.Interfaces.Sex;
using BindSharp;
using Infrastructure.Core.Interfaces.Catalog;

namespace Application.Core.Services.Sex;

public sealed class SexService : ISex
{
    private readonly ICatalogSnapshotStore _store;

    public SexService(ICatalogSnapshotStore store)
    {
        _store = store;
    }

    /// <inheritdoc/>
    public Task<Result<IEnumerable<GuidLookupItemResponse>, LookupError>> GetAllAsync() =>
        Task.FromResult(_store.Current is not { } catalog
            ? Result<IEnumerable<GuidLookupItemResponse>, LookupError>.Failure(new LookupCatalogUnavailableError())
            : Result<IEnumerable<GuidLookupItemResponse>, LookupError>.Success(
                catalog.Sexes.Values.Where(s => s.IsActive).OrderBy(s => s.Name)
                    .Select(s => new GuidLookupItemResponse { Code = s.Code, Name = s.Name }).ToList()));
}
