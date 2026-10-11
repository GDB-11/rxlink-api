using System.Text.Json;
using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.Prescription.Request;
using BindSharp;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Models.Catalog;

namespace Application.Core.Services.Prescription;

/// <summary>
/// Serialises prescription lines for the repository together with the catalog values as prescribed (§6.2 snapshots),
/// taken from the current catalog snapshot. Call after the codes have been validated.
/// </summary>
public sealed class PrescriptionLineSnapshotBuilder
{
    private readonly ICatalogSnapshotStore _store;

    public PrescriptionLineSnapshotBuilder(ICatalogSnapshotStore store) => _store = store;

    /// <summary>JSON array with the request fields plus the snapshot fields; fails when a code does not resolve.</summary>
    public Result<string, CatalogValidationError> BuildJson(IEnumerable<PrescriptionDetailRequest> details)
    {
        if (_store.Current is not { } catalog)
            return Result<string, CatalogValidationError>.Failure(new CatalogValidationUnavailableError());

        List<object> lines = [];
        foreach (PrescriptionDetailRequest d in details)
        {
            if (!catalog.Medications.TryGetValue(d.MedicationCode, out MedicationCatalogRow? medication))
                return NotFound(CatalogItemKind.Medication);
            if (!catalog.AdministrationRoutes.TryGetValue(d.AdministrationRouteCode, out AdministrationRouteCatalogRow? route))
                return NotFound(CatalogItemKind.AdministrationRoute);
            if (!catalog.Frequencies.TryGetValue(d.FrequencyCode, out FrequencyCatalogRow? frequency))
                return NotFound(CatalogItemKind.Frequency);

            catalog.PharmaceuticalForms.TryGetValue(medication.PharmaceuticalFormCode, out PharmaceuticalFormCatalogRow? form);

            lines.Add(new
            {
                d.MedicationCode,
                d.AdministrationRouteCode,
                d.FrequencyCode,
                d.Dose,
                d.DurationDays,
                d.Instructions,
                MedicationGenericName = medication.GenericName,
                MedicationCommercialName = medication.CommercialName,
                MedicationConcentration = medication.Concentration,
                PharmaceuticalFormName = form?.Name,
                AdministrationRouteName = route.Name,
                FrequencyDescription = frequency.Description,
                FrequencyIntervalHours = frequency.IntervalHours
            });
        }

        return Result<string, CatalogValidationError>.Success(JsonSerializer.Serialize(lines));
    }

    private static Result<string, CatalogValidationError> NotFound(CatalogItemKind kind) =>
        Result<string, CatalogValidationError>.Failure(new CatalogItemNotFoundError(kind));
}
