using Application.Core.DTOs.Medication.Errors;
using Application.Core.DTOs.Medication.Request;
using Application.Core.DTOs.Medication.Response;
using Application.Core.Interfaces.Medication;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Medication;
using Infrastructure.Core.Models.Medication;

namespace Application.Core.Services.Medication;

public sealed class MedicationService : IMedication
{
    private readonly IMedicationRepository _repository;

    public MedicationService(IMedicationRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task<Result<MedicationPageResponse, MedicationError>> GetPageAsync(MedicationPageRequest request)
    {
        int offset = (request.Page - 1) * request.PageSize;

        return _repository.GetPageAsync(offset, request.PageSize, request.Search)
            .MapErrorAsync(MedicationError (error) => new MedicationDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize));
    }

    /// <inheritdoc/>
    public Task<Result<MedicationResponse, MedicationError>> CreateAsync(CreateMedicationRequest request) =>
        _repository.InsertAsync(
                request.PharmaceuticalFormId,
                request.AdministrationRouteId,
                request.GenericName,
                request.CommercialName,
                request.Concentration)
            .MapErrorAsync(MedicationError (error) => new MedicationDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new MedicationDataAccessError("No se pudo registrar el medicamento."))
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<MedicationResponse, MedicationError>> UpdateAsync(Guid code, UpdateMedicationRequest request) =>
        _repository.UpdateAsync(
                code,
                request.PharmaceuticalFormId,
                request.AdministrationRouteId,
                request.GenericName,
                request.CommercialName,
                request.Concentration)
            .MapErrorAsync(MedicationError (error) => new MedicationDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new MedicationNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, MedicationError>> DeactivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.DeactivateAsync(code, performedByUserCode)
            .MapErrorAsync(MedicationError (error) => new MedicationDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new MedicationNotFoundError())
            .MapAsync(_ => Unit.Value);

    private static MedicationPageResponse BuildPageResponse(IEnumerable<MedicationRow> rows, int page, int pageSize)
    {
        List<MedicationRow> list       = rows.ToList();
        int                 totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;
        int                 totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new MedicationPageResponse
        {
            Items      = list.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize,
            TotalPages = totalPages
        };
    }

    private static MedicationResponse MapToResponse(MedicationRow row) =>
        new()
        {
            MedicationCode          = row.MedicationCode,
            PharmaceuticalFormId    = row.PharmaceuticalFormId,
            PharmaceuticalFormName  = row.PharmaceuticalFormName,
            AdministrationRouteId   = row.AdministrationRouteId,
            AdministrationRouteName = row.AdministrationRouteName,
            GenericName             = row.GenericName,
            CommercialName          = row.CommercialName,
            Concentration           = row.Concentration,
            IsActive                = row.IsActive
        };
}
