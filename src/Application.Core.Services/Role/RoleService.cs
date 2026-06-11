using Application.Core.DTOs.Role.Errors;
using Application.Core.DTOs.Role.Request;
using Application.Core.DTOs.Role.Response;
using Application.Core.Interfaces.Role;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Role;
using Infrastructure.Core.Models.Role;

namespace Application.Core.Services.Role;

public sealed class RoleService : IRole
{
    private readonly IRoleRepository _repository;

    public RoleService(IRoleRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public Task<Result<RolePageResponse, RoleError>> GetPageAsync(RolePageRequest request) =>
        _repository.GetPageAsync((request.Page - 1) * request.PageSize, request.PageSize, request.Search)
            .MapErrorAsync(RoleError (error) => new RoleDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize));

    /// <inheritdoc/>
    public Task<Result<RoleResponse, RoleError>> CreateAsync(CreateRoleRequest request) =>
        _repository.InsertAsync(request.Name, request.Description)
            .MapErrorAsync(RoleError (error) => new RoleDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new RoleDuplicateNameError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<RoleResponse, RoleError>> UpdateAsync(Guid code, UpdateRoleRequest request) =>
        _repository.UpdateAsync(code, request.Name, request.Description)
            .MapErrorAsync(RoleError (error) => new RoleDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new RoleNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, RoleError>> DeactivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.DeactivateAsync(code, performedByUserCode)
            .MapErrorAsync(RoleError (error) => new RoleDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new RoleNotFoundError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, RoleError>> ActivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.ActivateAsync(code, performedByUserCode)
            .MapErrorAsync(RoleError (error) => new RoleDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new RoleNotFoundError())
            .MapAsync(_ => Unit.Value);

    private static RolePageResponse BuildPageResponse(IEnumerable<RoleRow> rows, int page, int pageSize)
    {
        List<RoleRow> list = rows.ToList();
        int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;
        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new RolePageResponse
        {
            Items = list.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private static RoleResponse MapToResponse(RoleRow row) =>
        new()
        {
            RoleCode = row.RoleCode,
            Name = row.Name,
            Description = row.Description,
            IsActive = row.IsActive,
            CreatedAt = row.CreatedAt
        };
}