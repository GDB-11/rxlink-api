using System.Text.Json;
using Application.Core.DTOs.Patient.Response;
using Application.Core.Interfaces.Catalog;
using Application.Core.Interfaces.Patient;
using Infrastructure.Core.Models.Patient;

namespace Application.Core.Services.Patient;

public sealed class PatientAllergyResolver : IPatientAllergyResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ICatalogNames _names;

    public PatientAllergyResolver(ICatalogNames names)
    {
        _names = names;
    }

    /// <inheritdoc/>
    public PatientAllergyResponse Resolve(PatientAllergyRow row) =>
        new()
        {
            PatientAllergyCode = row.PatientAllergyCode,
            AllergyCode = row.AllergyCode,
            AllergyName = _names.Allergy(row.AllergyCode),
            SeverityCode = row.SeverityCode,
            SeverityName = row.SeverityCode is { } severity ? _names.AllergySeverity(severity) : null,
            Notes = row.Notes
        };

    /// <inheritdoc/>
    public List<PatientAllergyResponse> ResolveJson(string allergiesJson) =>
        (JsonSerializer.Deserialize<List<PatientAllergyRow>>(allergiesJson, JsonOptions) ?? [])
        .Select(Resolve)
        .OrderBy(a => a.AllergyName, StringComparer.CurrentCultureIgnoreCase)
        .ToList();
}
