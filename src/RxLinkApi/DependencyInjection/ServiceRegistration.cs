using Application.Core.Services.CommunicationPreference;
using Application.Core.Services.Disclosure;
using Application.Core.Services.PrivacyNotice;
using Application.Core.Services.Consent;
using Application.Core.Interfaces.PrivacyNotice;
using Application.Core.Interfaces.Consent;
using Application.Core.Services.Amendment;
using Application.Core.Interfaces.CommunicationPreference;
using Application.Core.Interfaces.Disclosure;
using Application.Core.Interfaces.Amendment;
using Application.Core.Interfaces.PatientExport;
using Application.Core.Services.PatientExport;
using Application.Core.Interfaces.Appointment;
using Application.Core.Interfaces.Availability;
using Application.Core.Interfaces.Diagnostic;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.ConsultationType;
using Application.Core.Interfaces.DocumentType;
using Application.Core.Interfaces.Inventory;
using Application.Core.Interfaces.Lookup;
using Application.Core.Interfaces.Navigation;
using Application.Core.Interfaces.Patient;
using Application.Core.Interfaces.PatientAccess;
using Application.Core.Interfaces.PatientAuth;
using Application.Core.Interfaces.Person;
using Application.Core.Interfaces.Prescription;
using Application.Core.Interfaces.Mfa;
using Application.Core.Interfaces.Report;
using Application.Core.Interfaces.Role;
using Application.Core.Interfaces.Sex;
using Application.Core.Interfaces.Catalog;
using Application.Core.Interfaces.Clinic;
using Application.Core.Interfaces.PartnerConfig;
using Application.Core.Interfaces.PartnerCatalog;
using Application.Core.Interfaces.Shared;
using Application.Core.Interfaces.Specialty;
using Application.Core.Interfaces.Users;
using Application.Core.Services.Appointment;
using Application.Core.Services.Availability;
using Application.Core.Services.Diagnostic;
using Application.Core.Services.Auth;
using Application.Core.Services.ConsultationType;
using Application.Core.Services.DocumentType;
using Application.Core.Services.Inventory;
using Application.Core.Services.Lookup;
using Application.Core.Services.Navigation;
using Application.Core.Services.Patient;
using Application.Core.Services.PatientAccess;
using Application.Core.Services.PatientAuth;
using Application.Core.Services.Person;
using Application.Core.Services.Prescription;
using Application.Core.Services.Mfa;
using Application.Core.Services.Report;
using Application.Core.Services.Role;
using Application.Core.Services.Sex;
using Application.Core.Services.Catalog;
using Application.Core.Services.Clinic;
using Application.Core.Services.PartnerConfig;
using Application.Core.Services.PartnerCatalog;
using Application.Core.Services.Shared;
using Application.Core.Services.Specialty;
using Application.Core.Services.Users;

namespace RxLinkApi.DependencyInjection;

internal static class ServiceRegistration
{
    extension(WebApplicationBuilder builder)
    {
        internal void RegisterApplicationServices()
        {
            builder.Services.AddScoped<IAppointment, AppointmentService>();
            builder.Services.AddScoped<IInsuranceTermsResolver, InsuranceTermsResolver>();
            builder.Services.AddScoped<IAvailability, AvailabilityService>();
            builder.Services.AddScoped<IPerson, PersonService>();
            builder.Services.AddScoped<IPatient, PatientService>();
            builder.Services.AddScoped<IPatientAllergyResolver, PatientAllergyResolver>();
            builder.Services.AddScoped<IPatientExport, PatientExportService>();
            builder.Services.AddScoped<IPatientAccessPolicy, PatientAccessPolicy>();
            builder.Services.AddScoped<IBreakGlass, BreakGlassService>();
            builder.Services.AddScoped<IJwt, JwtService>();
            builder.Services.AddScoped<IPassword, PasswordService>();
            builder.Services.AddScoped<IPasswordPolicy, PasswordPolicyService>();
            builder.Services.AddHttpClient<IBreachedPasswordChecker, PwnedPasswordsChecker>(client =>
            {
                client.BaseAddress = new Uri("https://api.pwnedpasswords.com/");
                client.Timeout = TimeSpan.FromSeconds(3);
            });
            builder.Services.AddScoped<IChaChaEncryption, ChaChaEncryptionService>();
            builder.Services.AddScoped<IDeterministicEncryption, DeterministicAesEncryptionService>();
            builder.Services.AddScoped<IEncryption, EncryptionService>();
            builder.Services.AddScoped<ITimeProvider, SystemTimeProviderService>();
            builder.Services.AddScoped<IAuthentication, AuthenticationService>();
            builder.Services.AddScoped<INavigation, NavigationService>();
            builder.Services.AddScoped<ILookup, LookupService>();
            builder.Services.AddScoped<IInventory, InventoryService>();
            builder.Services.AddScoped<IRole, RoleService>();
            builder.Services.AddScoped<IReport, ReportService>();
            builder.Services.AddScoped<ITotp, TotpService>();
            builder.Services.AddScoped<IMfa, MfaService>();
            builder.Services.AddScoped<IUser, UserService>();
            builder.Services.AddScoped<ISpecialty, SpecialtyService>();
            builder.Services.AddScoped<IClinic, ClinicService>();
            builder.Services.AddScoped<IAmendment, AmendmentService>();
            builder.Services.AddScoped<IDisclosure, DisclosureService>();
            builder.Services.AddScoped<IPrivacyNotice, PrivacyNoticeService>();
            builder.Services.AddScoped<IConsent, ConsentService>();
            builder.Services.AddScoped<ICommunicationPreference, CommunicationPreferenceService>();
            builder.Services.AddScoped<IPartnerCatalog, PartnerCatalogService>();
            builder.Services.AddScoped<ICatalogWriteValidator, CatalogWriteValidator>();
            builder.Services.AddScoped<PrescriptionLineSnapshotBuilder>();
            builder.Services.AddScoped<ICatalogView, CatalogViewService>();
            builder.Services.AddSingleton<ICatalogNames, CatalogNames>();
            builder.Services.AddScoped<IPartnerConfig, PartnerConfigService>();
            builder.Services.AddScoped<IPartnerIdentityVerifier, PartnerIdentityVerifier>();
            builder.Services.AddScoped<IDiagnostic, DiagnosticService>();
            builder.Services.AddScoped<IPrescription, PrescriptionService>();
            builder.Services.AddScoped<IPrescriptionSafety, PrescriptionSafetyService>();
            builder.Services.AddScoped<IPatientAuthentication, PatientAuthenticationService>();
            builder.Services.AddScoped<ISex, SexService>();
            builder.Services.AddScoped<IDocumentType, DocumentTypeService>();
            builder.Services.AddScoped<IConsultationType, ConsultationTypeService>();
        }
    }
}