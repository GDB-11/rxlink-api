using Microsoft.OpenApi;

namespace RxLinkApi.DependencyInjection;

/// <summary>
/// The patient (mobile) contract: every route under <c>/api/v1/</c>, published as its own OpenAPI document.
/// The committed copy (<c>docs/openapi-patient-v1.json</c>) is what the app is built against, and a test fails
/// when the controllers stop producing exactly that file, so the contract cannot change by accident.
/// </summary>
public static class PatientContractOpenApi
{
    public const string DocumentName = "patient-v1";

    public static IServiceCollection AddPatientContractOpenApi(this IServiceCollection services) =>
        services.AddOpenApi(DocumentName, options =>
        {
            options.ShouldInclude = description =>
                description.RelativePath?.StartsWith("api/v1/", StringComparison.Ordinal) == true;

            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "RxLink patient API",
                    Version = "1.0.0",
                    Description = "Contract of the RxLink mobile app. Codes (UUIDs) only; no serial ids."
                };
                return Task.CompletedTask;
            });
        });
}
