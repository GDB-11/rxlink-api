using Application.Core.DTOs.User.Errors;
using Application.Core.DTOs.User.Request;
using Application.Core.DTOs.User.Response;
using Application.Core.Interfaces.Auth;
using BindSharp;

namespace Application.Core.Interfaces.Users;

public interface IUser
{
    Task<Result<UserPageResponse, UserError>> GetPageAsync(UserPageRequest request);
    Task<Result<UserResponse, UserError>> CreateAsync(CreateUserRequest request);
    Task<Result<UserResponse, UserError>> UpdateAsync(Guid code, UpdateUserRequest request);
    Task<Result<UserResponse, UserError>> UpdateRoleAsync(Guid code, UpdateUserRoleRequest request);
    Task<Result<Unit, UserError>> DeactivateAsync(Guid code, Guid performedByUserCode);
    Task<Result<Unit, UserError>> ActivateAsync(Guid code, Guid performedByUserCode);
}
