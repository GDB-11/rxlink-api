using Application.Core.DTOs.Role.Errors;
using Application.Core.DTOs.Role.Request;
using Application.Core.DTOs.Role.Response;
using BindSharp;

namespace Application.Core.Interfaces.Role;

public interface IRole
{
    Task<Result<RolePageResponse, RoleError>> GetPageAsync(RolePageRequest request);
    Task<Result<RoleResponse, RoleError>> CreateAsync(CreateRoleRequest request);
    Task<Result<RoleResponse, RoleError>> UpdateAsync(Guid code, UpdateRoleRequest request);
    Task<Result<Unit, RoleError>> DeactivateAsync(Guid code, Guid performedByUserCode);
    Task<Result<Unit, RoleError>> ActivateAsync(Guid code, Guid performedByUserCode);
}