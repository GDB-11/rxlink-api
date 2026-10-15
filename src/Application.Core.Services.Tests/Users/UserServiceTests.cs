using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Catalog;
using Application.Core.Services.Users;
using BindSharp;
using Infrastructure.Core.DTOs.User;
using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Users;
using Infrastructure.Core.Models.Users;
using NSubstitute;

namespace Application.Core.Services.Tests.Users;

public sealed class UserServiceTests
{
    private readonly IUserRepository _repository = Substitute.For<IUserRepository>();
    private readonly ICatalogNames _names = Substitute.For<ICatalogNames>();
    private readonly UserService _sut;

    private readonly Guid _sex = Guid.NewGuid();
    private readonly Guid _docType = Guid.NewGuid();
    private readonly Guid _specialty = Guid.NewGuid();

    public UserServiceTests()
    {
        _names.Sex(_sex).Returns("Masculino");
        _names.DocumentType(_docType).Returns("DNI");
        _names.Specialty(_specialty).Returns("Cardiología");
        _sut = new UserService(_repository, Substitute.For<IPassword>(), Substitute.For<IActivityLogger>(),
            Substitute.For<IPasswordPolicy>(), Substitute.For<ICatalogWriteValidator>(), _names);
    }

    private UserRow Row(Guid? docType, Guid? specialty) => new()
    {
        UserCode = Guid.NewGuid(), PersonCode = Guid.NewGuid(), Names = "Ana", Surnames = "Pérez",
        BirthDate = new DateOnly(1990, 1, 1), SexCode = _sex, Phone = "999", PersonEmail = "a@b.pe",
        DocumentTypeCode = docType, RoleCode = Guid.NewGuid(), RoleName = "Doctor", SpecialtyCode = specialty,
        Username = "ana", Email = "a@b.pe", IsActive = true, CreatedAt = DateTime.UtcNow, TotalCount = 1
    };

    [Fact]
    public async Task GetByCode_ResolvesSexDocumentTypeAndSpecialtyNamesFromCatalog()
    {
        UserRow row = Row(_docType, _specialty);
        _repository.GetByCodeAsync(row.UserCode)
            .Returns(Result<UserRow?, UserRepositoryError>.Success(row));

        var result = await _sut.GetByCodeAsync(row.UserCode);

        Assert.True(result.IsSuccess);
        Assert.Equal("Masculino", result.Value.SexName);
        Assert.Equal("DNI", result.Value.DocumentTypeName);
        Assert.Equal("Cardiología", result.Value.SpecialtyName);
    }

    [Fact]
    public async Task GetByCode_LeavesOptionalNamesNullWhenCodesAreAbsent()
    {
        UserRow row = Row(null, null);
        _repository.GetByCodeAsync(row.UserCode)
            .Returns(Result<UserRow?, UserRepositoryError>.Success(row));

        var result = await _sut.GetByCodeAsync(row.UserCode);

        Assert.Null(result.Value.DocumentTypeName);
        Assert.Null(result.Value.SpecialtyName);
    }
}
