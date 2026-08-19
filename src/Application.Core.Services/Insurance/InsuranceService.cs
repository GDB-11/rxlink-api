using Application.Core.DTOs.Insurance.Errors;
using Application.Core.DTOs.Insurance.Request;
using Application.Core.DTOs.Insurance.Response;
using Application.Core.Interfaces.Insurance;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Insurance;
using Infrastructure.Core.Models.Insurance;

namespace Application.Core.Services.Insurance;

public sealed class InsuranceService : IInsurance
{
    private readonly IInsuranceRepository _repository;

    public InsuranceService(IInsuranceRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task<Result<InsurancePageResponse, InsuranceError>> GetPageAsync(InsurancePageRequest request) =>
        _repository.GetPageAsync((request.Page - 1) * request.PageSize, request.PageSize, request.Search)
            .MapErrorAsync(InsuranceError (error) =>
                new InsuranceDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize));

    /// <inheritdoc/>
    public Task<Result<InsuranceResponse, InsuranceError>> CreateAsync(CreateInsuranceRequest request) =>
        _repository.InsertAsync(request.Name, request.CoveragePercentage)
            .MapErrorAsync(InsuranceError (error) =>
                new InsuranceDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new InsuranceDataAccessError("No se pudo registrar el seguro."))
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<InsuranceResponse, InsuranceError>> UpdateAsync(Guid code, UpdateInsuranceRequest request) =>
        _repository.UpdateAsync(code, request.Name, request.CoveragePercentage)
            .MapErrorAsync(InsuranceError (error) =>
                new InsuranceDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new InsuranceNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, InsuranceError>> DeactivateAsync(Guid code) =>
        _repository.DeactivateAsync(code)
            .MapErrorAsync(InsuranceError (error) =>
                new InsuranceDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new InsuranceNotFoundError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, InsuranceError>> ActivateAsync(Guid code) =>
        _repository.ActivateAsync(code)
            .MapErrorAsync(InsuranceError (error) =>
                new InsuranceDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new InsuranceNotFoundError())
            .MapAsync(_ => Unit.Value);

    private static InsurancePageResponse BuildPageResponse(IEnumerable<InsuranceRow> rows, int page, int pageSize)
    {
        List<InsuranceRow> list = rows.ToList();
        int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;
        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new InsurancePageResponse
        {
            Items = list.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private static InsuranceResponse MapToResponse(InsuranceRow row) =>
        new()
        {
            InsuranceCode = row.InsuranceCode,
            Name = row.Name,
            CoveragePercentage = row.CoveragePercentage,
            IsActive = row.IsActive,
        };
}
