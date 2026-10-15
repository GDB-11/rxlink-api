using Application.Core.DTOs.Catalog.Errors;
using Application.Core.Interfaces.Appointment;
using Application.Core.Interfaces.Catalog;
using BindSharp;
using Infrastructure.Core.DTOs.Appointment;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.PartnerCatalog;
using Infrastructure.Core.Models.Catalog;
using Infrastructure.Core.Models.PartnerCatalog;

namespace Application.Core.Services.Appointment;

public sealed class InsuranceTermsResolver : IInsuranceTermsResolver
{
    private readonly ICatalogWriteValidator _validator;
    private readonly ICatalogSnapshotStore _store;
    private readonly IPartnerCatalogRepository _partner;

    public InsuranceTermsResolver(
        ICatalogWriteValidator validator, ICatalogSnapshotStore store, IPartnerCatalogRepository partner)
    {
        _validator = validator;
        _store = store;
        _partner = partner;
    }

    /// <inheritdoc/>
    public async Task<Result<InsuranceTerms?, CatalogValidationError>> ResolveAsync(Guid? insuranceCode)
    {
        if (insuranceCode is not { } code)
            return Result<InsuranceTerms?, CatalogValidationError>.Success(null);

        Result<Unit, CatalogValidationError> validation = await _validator.ValidateInsuranceAsync(code);
        if (!validation.IsSuccess)
            return Result<InsuranceTerms?, CatalogValidationError>.Failure(validation.Error);

        if (_store.Current is not { } catalog || !catalog.Insurances.TryGetValue(code, out InsuranceCatalogRow? insurance))
            return Result<InsuranceTerms?, CatalogValidationError>.Failure(new CatalogValidationUnavailableError());

        var setting = await _partner.GetInsuranceAsync(code);
        if (!setting.IsSuccess)
            return Result<InsuranceTerms?, CatalogValidationError>.Failure(
                new CatalogValidationDataAccessError(setting.Error.Message, setting.Error.Details, setting.Error.Exception));

        decimal coverage = setting.Value.CoveragePercentageOverride ?? insurance.DefaultCoveragePercentage;
        return Result<InsuranceTerms?, CatalogValidationError>.Success(new InsuranceTerms(code, insurance.Name, coverage));
    }
}
