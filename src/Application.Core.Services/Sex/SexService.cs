using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Lookup.Response;
using Application.Core.Interfaces.Sex;
using BindSharp;
using Infrastructure.Core.Interfaces.Lookup;
using Infrastructure.Core.Models.Lookup;

namespace Application.Core.Services.Sex;

public sealed class SexService : ISex
{
    private readonly ILookupRepository _repository;

    public SexService(ILookupRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task<Result<IEnumerable<GuidLookupItemResponse>, LookupError>> GetAllAsync() =>
        _repository.GetSexesAsync()
            .MapErrorAsync(LookupError (e) => new LookupDataAccessError(e.Message, e.Details, e.Exception))
            .MapAsync(rows => rows.Select(ToResponse));

    private static GuidLookupItemResponse ToResponse(GuidLookupRow row) =>
        new() { Code = row.Code, Name = row.Name };
}
