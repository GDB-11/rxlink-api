using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using RxLinkApi.Controllers;
using RxLinkApi.DependencyInjection;

namespace Application.Core.Services.Tests.Contract;

/// <summary>
/// Freezes the mobile contract: the OpenAPI document generated from the controllers must equal the committed
/// <c>docs/openapi-patient-v1.json</c>. After an intentional change, regenerate it with
/// <c>UPDATE_CONTRACT=1 dotnet test --filter PatientContractTests</c> and review the diff.
/// </summary>
public sealed class PatientContractTests
{
    private const string ContractPath = "docs/openapi-patient-v1.json";

    [Fact]
    public async Task GeneratedPatientDocument_MatchesTheCommittedContract()
    {
        string generated = Normalize(await GenerateAsync());
        string path = Path.Combine(FindRepositoryRoot(), ContractPath);

        if (Environment.GetEnvironmentVariable("UPDATE_CONTRACT") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, generated);
        }

        Assert.True(File.Exists(path), $"{ContractPath} is missing; generate it with UPDATE_CONTRACT=1.");
        Assert.Equal(Normalize(await File.ReadAllTextAsync(path)), generated);
    }

    [Fact]
    public async Task PatientContract_ContainsOnlyVersionedRoutes_AndNoStaffRoutes()
    {
        string json = await GenerateAsync();
        using var document = System.Text.Json.JsonDocument.Parse(json);
        string[] paths = document.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToArray();

        Assert.NotEmpty(paths);
        Assert.All(paths, p => Assert.StartsWith("/api/v1/", p));
        Assert.DoesNotContain(paths, p => p.Contains("/admin/", StringComparison.Ordinal));
        Assert.Contains("/api/v1/patient-auth/lookup", paths);
        Assert.Contains("/api/v1/partner/config/public", paths);
    }

    private static async Task<string> GenerateAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers().AddApplicationPart(typeof(PatientAuthController).Assembly);
        builder.Services.AddPatientContractOpenApi();

        await using WebApplication app = builder.Build();
        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>(PatientContractOpenApi.DocumentName);

        OpenApiDocument document = await provider.GetOpenApiDocumentAsync();
        return await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1);
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd() + "\n";

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("rxlink-api root not found.");
    }
}
