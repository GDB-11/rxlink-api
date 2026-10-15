using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Catalog;
using Application.Core.Services.Person;
using BindSharp;
using Infrastructure.Core.DTOs.Person;
using Infrastructure.Core.Interfaces.Person;
using Infrastructure.Core.Models.Person;
using NSubstitute;

namespace Application.Core.Services.Tests.Person;

public sealed class PersonServiceTests
{
    private readonly IPersonRepository _repository = Substitute.For<IPersonRepository>();
    private readonly ICatalogNames _names = Substitute.For<ICatalogNames>();
    private readonly PersonService _sut;

    private readonly Guid _sex = Guid.NewGuid();
    private readonly Guid _docType = Guid.NewGuid();

    public PersonServiceTests()
    {
        _names.Sex(_sex).Returns("Femenino");
        _names.DocumentType(_docType).Returns("DNI");
        _sut = new PersonService(_repository, Substitute.For<IPassword>(),
            Substitute.For<ICatalogWriteValidator>(), _names);
    }

    private PersonRow Row(Guid? docType) => new()
    {
        PersonCode = Guid.NewGuid(), Names = "Ana", Surnames = "Pérez", BirthDate = new DateOnly(1990, 1, 1),
        SexCode = _sex, Phone = "999", Email = "a@b.pe", DocumentTypeCode = docType, DocumentNumber = "123",
        TotalCount = 1
    };

    [Fact]
    public async Task GetByCode_ResolvesSexAndDocumentTypeNamesFromCatalog()
    {
        PersonRow row = Row(_docType);
        _repository.GetByCodeAsync(row.PersonCode)
            .Returns(Result<PersonRow?, PersonRepositoryError>.Success(row));

        var result = await _sut.GetByCodeAsync(row.PersonCode);

        Assert.True(result.IsSuccess);
        Assert.Equal("Femenino", result.Value.SexName);
        Assert.Equal("DNI", result.Value.DocumentTypeName);
    }

    [Fact]
    public async Task GetByCode_LeavesDocumentTypeNameNullWithoutDocument()
    {
        PersonRow row = Row(null);
        _repository.GetByCodeAsync(row.PersonCode)
            .Returns(Result<PersonRow?, PersonRepositoryError>.Success(row));

        var result = await _sut.GetByCodeAsync(row.PersonCode);

        Assert.Null(result.Value.DocumentTypeName);
    }
}
