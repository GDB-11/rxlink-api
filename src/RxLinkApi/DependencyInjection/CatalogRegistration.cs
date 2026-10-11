using Application.Core.Config;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Models.Catalog;
using Infrastructure.Core.Services.Catalog;

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

            services.AddSingleton<ICatalogSnapshotStore, CatalogSnapshotStore>();
            services.AddHostedService<CatalogSnapshotRefresher>();
            services.AddHealthChecks().AddCheck<CatalogHealthCheck>("catalog", tags: ["ready"]);
        }
    }

    private static void AddCatalogReader<TRow>(this IServiceCollection services, string sql) =>
        services.AddSingleton<ICatalogReader<TRow>>(sp =>
            new CatalogReader<TRow>(sp.GetRequiredService<ICatalogConnectionFactory>(), sql));
}
