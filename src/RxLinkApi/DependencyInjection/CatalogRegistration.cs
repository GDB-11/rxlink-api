using Application.Core.Config;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.PartnerConfig;
using Infrastructure.Core.Models.Catalog;
using Infrastructure.Core.Services.Catalog;
using Infrastructure.Core.Services.PartnerConfig;

namespace RxLinkApi.DependencyInjection;

internal static class CatalogRegistration
{
    extension(WebApplicationBuilder builder)
    {
        internal void RegisterCatalog()
        {
            IServiceCollection services = builder.Services;

            CatalogConfig defaults = new();
            services.AddSingleton(new CatalogConfig
            {
                RefreshSeconds = builder.Configuration.GetValue("Catalog:RefreshSeconds", defaults.RefreshSeconds)
            });

            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<ICatalogConnectionFactory>(new NpgsqlCatalogConnectionFactory(
                builder.Configuration.GetConnectionString("CatalogConnection")));

            services.AddSingleton<ICatalogVersionReader, CatalogVersionReader>();
            services.AddSingleton<IPartnerRegistryReader, PartnerRegistryReader>();
            services.AddCatalogReader<DocumentTypeCatalogRow>(CatalogReaderSql.DocumentTypes);
            services.AddCatalogReader<SexCatalogRow>(CatalogReaderSql.Sexes);
            services.AddCatalogReader<SpecialtyCatalogRow>(CatalogReaderSql.Specialties);
            services.AddCatalogReader<PharmaceuticalFormCatalogRow>(CatalogReaderSql.PharmaceuticalForms);
            services.AddCatalogReader<AdministrationRouteCatalogRow>(CatalogReaderSql.AdministrationRoutes);
            services.AddCatalogReader<FrequencyCatalogRow>(CatalogReaderSql.Frequencies);
            services.AddCatalogReader<AllergyCatalogRow>(CatalogReaderSql.Allergies);
            services.AddCatalogReader<AllergySeverityCatalogRow>(CatalogReaderSql.AllergySeverities);
            services.AddCatalogReader<MedicationCatalogRow>(CatalogReaderSql.Medications);
            services.AddCatalogReader<InsuranceCatalogRow>(CatalogReaderSql.Insurances);
            services.AddCatalogReader<MedicationClassCatalogRow>(CatalogReaderSql.MedicationClasses);
            services.AddCatalogReader<MedicationClassMemberCatalogRow>(CatalogReaderSql.MedicationClassMembers);
            services.AddCatalogReader<AllergyMedicationClassCatalogRow>(CatalogReaderSql.AllergyMedicationClasses);
            services.AddCatalogReader<MedicationInteractionCatalogRow>(CatalogReaderSql.MedicationInteractions);

            services.AddSingleton<ICatalogSnapshotStore, CatalogSnapshotStore>();
            services.AddHostedService<PartnerIdentityGuard>();
            services.AddHostedService<CatalogSnapshotRefresher>();
            services.RegisterHeartbeat(builder.Configuration);
            services.AddHealthChecks().AddCheck<CatalogHealthCheck>("catalog", tags: ["ready"]);
        }
    }

    private static void RegisterHeartbeat(this IServiceCollection services, IConfiguration configuration)
    {
        CatalogApiConfig defaults = new();
        CatalogApiConfig config = new()
        {
            BaseUrl = configuration["CatalogApi:BaseUrl"],
            ApiKey = configuration["CatalogApi:ApiKey"],
            HeartbeatMinutes = configuration.GetValue("CatalogApi:HeartbeatMinutes", defaults.HeartbeatMinutes),
            SupportedCatalogSchemaMin = configuration.GetValue("CatalogApi:SupportedCatalogSchemaMin", defaults.SupportedCatalogSchemaMin),
            SupportedCatalogSchemaMax = configuration.GetValue("CatalogApi:SupportedCatalogSchemaMax", defaults.SupportedCatalogSchemaMax)
        };
        services.AddSingleton(config);

        // Not configured (no URL or key): no heartbeat at all, which is the normal state until Phase 8 ships.
        if (!config.IsConfigured) return;

        services.AddSingleton<IPartnerHeartbeatClient>(_ => new PartnerHeartbeatClient(
            new HttpClient { BaseAddress = new Uri(config.BaseUrl!.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(10) },
            config.ApiKey!));
        services.AddHostedService<PartnerHeartbeatService>();
    }

    private static void AddCatalogReader<TRow>(this IServiceCollection services, string sql) =>
        services.AddSingleton<ICatalogReader<TRow>>(sp =>
            new CatalogReader<TRow>(sp.GetRequiredService<ICatalogConnectionFactory>(), sql));
}
