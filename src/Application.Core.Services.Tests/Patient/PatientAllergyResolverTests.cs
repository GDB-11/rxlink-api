using Application.Core.Interfaces.Catalog;
using Application.Core.Services.Patient;
using NSubstitute;

namespace Application.Core.Services.Tests.Patient;

public sealed class PatientAllergyResolverTests
{
    private readonly ICatalogNames _names = Substitute.For<ICatalogNames>();
    private readonly PatientAllergyResolver _sut;

    private readonly Guid _penicillin = Guid.NewGuid();
    private readonly Guid _aspirin = Guid.NewGuid();
    private readonly Guid _severe = Guid.NewGuid();

    public PatientAllergyResolverTests()
    {
        _names.Allergy(_penicillin).Returns("Penicilina");
        _names.Allergy(_aspirin).Returns("Aspirina");
        _names.AllergySeverity(_severe).Returns("Grave");
        _sut = new PatientAllergyResolver(_names);
    }

    [Fact]
    public void ResolveJson_ResolvesNamesFromCatalogAndOrdersByAllergyName()
    {
        string json = $$"""
            [{"PatientAllergyCode":"{{Guid.NewGuid()}}","AllergyCode":"{{_penicillin}}","SeverityCode":"{{_severe}}","Notes":"n"},
             {"PatientAllergyCode":"{{Guid.NewGuid()}}","AllergyCode":"{{_aspirin}}","SeverityCode":null,"Notes":null}]
            """;

        var result = _sut.ResolveJson(json);

        Assert.Equal(["Aspirina", "Penicilina"], result.Select(a => a.AllergyName));
        Assert.Null(result[0].SeverityName);
        Assert.Equal("Grave", result[1].SeverityName);
        Assert.Equal("n", result[1].Notes);
    }

    [Fact]
    public void ResolveJson_EmptyArray_ReturnsEmptyList() =>
        Assert.Empty(_sut.ResolveJson("[]"));
}
