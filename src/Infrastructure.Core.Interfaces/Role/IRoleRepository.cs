using BindSharp;
using Infrastructure.Core.DTOs.Role;
using Infrastructure.Core.Models.Role;

namespace Infrastructure.Core.Interfaces.Role;

public interface IRoleRepository
{
    Task<Result<IEnumerable<RoleRow>, RoleRepositoryError>> GetPageAsync(int offset, int limit, string? search);

    Task<Result<RoleRow?, RoleRepositoryError>> InsertAsync(string name, string? description);

    Task<Result<RoleRow?, RoleRepositoryError>> UpdateAsync(Guid code, string name, string? description);

    Task<Result<int, RoleRepositoryError>> DeactivateAsync(Guid code, Guid performedByUserCode);

    Task<Result<int, RoleRepositoryError>> ActivateAsync(Guid code, Guid performedByUserCode);
}