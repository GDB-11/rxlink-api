using Application.Core.DTOs.Allergy.Errors;
using Application.Core.DTOs.Allergy.Request;
using Application.Core.DTOs.Allergy.Response;
using Application.Core.Interfaces.Allergy;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Allergy;
using Infrastructure.Core.Models.Allergy;

namespace Application.Core.Services.Allergy;

public sealed class AllergyService : IAllergy
{
    private readonly IAllergyRepository _repository;

    public AllergyService(IAllergyRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task<Result<AllergyPageResponse, AllergyError>> GetPageAsync(AllergyPageRequest request)
    {
        int offset = (request.Page - 1) * request.PageSize;

        return _repository.GetPageAsync(offset, request.PageSize, request.Search)
            .MapErrorAsync(AllergyError (error) =>
                new AllergyDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize));
    }

    /// <inheritdoc/>
    public Task<Result<AllergyResponse, AllergyError>> CreateAsync(CreateAllergyRequest request) =>
        _repository.InsertAsync(request.Name, request.Description)
            .MapErrorAsync(AllergyError (error) =>
                new AllergyDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new AllergyDataAccessError("No se pudo registrar la alergia."))
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<AllergyResponse, AllergyError>> UpdateAsync(Guid code, UpdateAllergyRequest request) =>
        _repository.UpdateAsync(code, request.Name, request.Description)
            .MapErrorAsync(AllergyError (error) =>
                new AllergyDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new AllergyNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, AllergyError>> DeactivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.DeactivateAsync(code, performedByUserCode)
            .MapErrorAsync(AllergyError (error) =>
                new AllergyDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new AllergyNotFoundError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, AllergyError>> ActivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.ActivateAsync(code)
            .MapErrorAsync(AllergyError (error) =>
                new AllergyDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new AllergyNotFoundError())
            .MapAsync(_ => Unit.Value);

    private static AllergyPageResponse BuildPageResponse(IEnumerable<AllergyRow> rows, int page, int pageSize)
    {
        List<AllergyRow> list = rows.ToList();
        int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;
        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new AllergyPageResponse
        {
            Items = list.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private static AllergyResponse MapToResponse(AllergyRow row) =>
        new()
        {
            AllergyCode = row.AllergyCode,
            Name = row.Name,
            Description = row.Description,
            IsActive = row.IsActive
        };
}