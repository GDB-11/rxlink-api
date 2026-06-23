using Application.Core.DTOs.User.Errors;
using Application.Core.DTOs.User.Request;
using Application.Core.DTOs.User.Response;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Users;
using BindSharp;
using BindSharp.Extensions;
using Infrastructure.Core.Interfaces.Users;
using Infrastructure.Core.Models.Users;

namespace Application.Core.Services.Users;

public sealed class UserService : IUser
{
    private readonly IUserRepository _repository;
    private readonly IPassword _password;

    public UserService(IUserRepository repository, IPassword password)
    {
        _repository = repository;
        _password = password;
    }

    /// <inheritdoc/>
    public Task<Result<UserPageResponse, UserError>> GetPageAsync(UserPageRequest request)
    {
        int offset = (request.Page - 1) * request.PageSize;

        return _repository.GetPageAsync(offset, request.PageSize, request.Search, request.Role, request.SpecialtyCode)
            .MapErrorAsync(UserError (error) => new UserDataAccessError(error.Message, error.Details, error.Exception))
            .MapAsync(rows => BuildPageResponse(rows, request.Page, request.PageSize));
    }

    /// <inheritdoc/>
    public Task<Result<UserResponse, UserError>> GetByCodeAsync(Guid code) =>
        _repository.GetByCodeAsync(code)
            .MapErrorAsync(UserError (error) => new UserDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new UserNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<UserResponse, UserError>> CreateAsync(CreateUserRequest request) =>
        _password.HashPassword(request.Password)
            .MapError(UserError (_) => new UserPasswordError())
            .BindAsync(passwordHash => _repository.InsertAsync(
                    personCode: request.PersonCode,
                    roleName: request.RoleName,
                    specialtyCode: request.SpecialtyCode,
                    username: request.Username,
                    email: request.Email,
                    passwordHash: passwordHash,
                    licenseNumber: request.LicenseNumber)
                .MapErrorAsync(UserError (error) =>
                    new UserDataAccessError(error.Message, error.Details, error.Exception))
                .EnsureNotNullAsync(new UserRoleNotFoundError())
                .MapAsync(MapToResponse));

    /// <inheritdoc/>
    public Task<Result<UserResponse, UserError>> UpdateAsync(Guid code, UpdateUserRequest request) =>
        _repository.UpdateAsync(
                code: code,
                roleName: request.RoleName,
                specialtyCode: request.SpecialtyCode,
                username: request.Username,
                email: request.Email,
                licenseNumber: request.LicenseNumber)
            .MapErrorAsync(UserError (error) => new UserDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new UserNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<UserResponse, UserError>> UpdateRoleAsync(Guid code, UpdateUserRoleRequest request) =>
        _repository.UpdateRoleAsync(code, request.RoleName)
            .MapErrorAsync(UserError (error) => new UserDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new UserNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, UserError>> DeactivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.DeactivateAsync(code, performedByUserCode)
            .MapErrorAsync(UserError (error) => new UserDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new UserNotFoundError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<Unit, UserError>> ActivateAsync(Guid code, Guid performedByUserCode) =>
        _repository.ActivateAsync(code)
            .MapErrorAsync(UserError (error) => new UserDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureAsync(affected => affected > 0, new UserNotFoundError())
            .MapAsync(_ => Unit.Value);

    /// <inheritdoc/>
    public Task<Result<UserResponse, UserError>> GetMyProfileAsync(Guid userCode) =>
        _repository.GetByCodeAsync(userCode)
            .MapErrorAsync(UserError (error) => new UserDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new UserNotFoundError())
            .MapAsync(MapToResponse);

    /// <inheritdoc/>
    public Task<Result<Unit, UserError>> ChangePasswordAsync(Guid userCode, ChangePasswordRequest request) =>
        _repository.GetPasswordHashAsync(userCode)
            .MapErrorAsync(UserError (error) => new UserDataAccessError(error.Message, error.Details, error.Exception))
            .EnsureNotNullAsync(new UserNotFoundError())
            .BindAsync(currentHash =>
                _password.VerifyPassword(request.CurrentPassword, currentHash)
                    .MapError(UserError (_) => new UserPasswordError())
                    .Ensure(isValid => isValid, new UserInvalidCurrentPasswordError())
                    .Bind(_ => _password.HashPassword(request.NewPassword)
                        .MapError(UserError (_) => new UserPasswordError()))
                    .BindAsync(newHash =>
                        _repository.UpdatePasswordAsync(userCode, newHash)
                            .MapErrorAsync(UserError (error) =>
                                new UserDataAccessError(error.Message, error.Details, error.Exception))
                            .EnsureAsync(affected => affected > 0, new UserNotFoundError())
                            .MapAsync(_ => Unit.Value)));

    private static UserPageResponse BuildPageResponse(IEnumerable<UserRow> rows, int page, int pageSize)
    {
        List<UserRow> list = rows.ToList();
        int totalCount = list.Count > 0 ? (int)list[0].TotalCount : 0;
        int totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new UserPageResponse
        {
            Items = list.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    private static UserResponse MapToResponse(UserRow row) =>
        new()
        {
            UserCode = row.UserCode,
            PersonCode = row.PersonCode,
            Names = row.Names,
            Surnames = row.Surnames,
            BirthDate = row.BirthDate,
            SexCode = row.SexCode,
            SexName = row.SexName,
            Phone = row.Phone,
            AlternativePhone = row.AlternativePhone,
            PersonEmail = row.PersonEmail,
            Address = row.Address,
            EmergencyContactName = row.EmergencyContactName,
            EmergencyContactPhone = row.EmergencyContactPhone,
            DocumentTypeCode = row.DocumentTypeCode,
            DocumentTypeName = row.DocumentTypeName,
            DocumentNumber = row.DocumentNumber,
            DocumentIssueDate = row.DocumentIssueDate,
            DocumentExpirationDate = row.DocumentExpirationDate,
            RoleCode = row.RoleCode,
            RoleName = row.RoleName,
            SpecialtyCode = row.SpecialtyCode,
            SpecialtyName = row.SpecialtyName,
            Username = row.Username,
            Email = row.Email,
            LicenseNumber = row.LicenseNumber,
            IsActive = row.IsActive,
            CreatedAt = row.CreatedAt
        };
}