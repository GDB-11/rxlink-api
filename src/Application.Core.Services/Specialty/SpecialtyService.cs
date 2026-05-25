using Application.Core.DTOs.Specialty.Errors;
using Application.Core.DTOs.Specialty.Request;
using Application.Core.DTOs.Specialty.Response;
using Application.Core.Interfaces.Specialty;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Specialty;
using Infrastructure.Core.Models.Specialty;

namespace Application.Core.Services.Specialty;

public sealed class SpecialtyService : ISpecialty
{
    private readonly ISpecialtyRepository _repository;

    public SpecialtyService(ISpecialtyRepository repository)
    {
        _repository = repository;
    }
    
    
    /// <inheritdoc/>
    public Task<Result<SpecialtyPageResponse, SpecialtyError>> GetPageAsync(SpecialtyPageRequest request)
    {
        int offset = (request.Page - 1) * request.PageSize;

        return _repository.GetPageAsync(offset, request.PageSize, request.Search)
            .MapErrorAsync(SpecialtyError (error) => new SpecialtyDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize));
    }

    /// <inheritdoc/>
    public Task<Result<SpecialtyResponse, SpecialtyError>> CreateAsync(CreateSpecialtyRequest request) =>
            _repository.InsertAsync(request.Name)
                .MapErrorAsync(SpecialtyError (error) => new SpecialtyDataAccessError(error.Message, error.Details, error.Exception))
                .EnsureNotNullAsync(new SpecialtyDataAccessError("No se pudo registrar la especialidad."))
                .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<SpecialtyResponse, SpecialtyError>> UpdateAsync(Guid code, UpdateSpecialtyRequest request) =>
        _repository.UpdateAsync(code, request.Name)
            .MapErrorAsync(SpecialtyError (error) => new SpecialtyDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new SpecialtyNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, SpecialtyError>> DeactivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.DeactivateAsync(code, performedByUserCode)
            .MapErrorAsync(SpecialtyError (error) => new SpecialtyDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new SpecialtyNotFoundError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, SpecialtyError>> ActivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.ActivateAsync(code)
            .MapErrorAsync(SpecialtyError (error) => new SpecialtyDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new SpecialtyNotFoundError())
            .MapAsync(_ => Unit.Value);
    


    private static SpecialtyPageResponse BuildPageResponse(IEnumerable<SpecialtyRow> rows, int page, int pageSize)
    {
        List<SpecialtyRow> list = rows.ToList();
        int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;
        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new SpecialtyPageResponse
        {
            Items = list.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }
    
    private static SpecialtyResponse MapToResponse(SpecialtyRow row) =>
        new()
        {
            SpecialtyCode = row.SpecialtyCode,
            Name = row.Name,
            IsActive = row.IsActive,
        };
}