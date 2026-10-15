using Application.Core.DTOs.CommunicationPreference.Errors;
using Application.Core.DTOs.Disclosure.Errors;
using Application.Core.DTOs.PrivacyNotice.Errors;
using Application.Core.DTOs.Consent.Errors;
using Application.Core.DTOs.Amendment.Errors;
using Application.Core.DTOs.Appointment.Errors;
using Application.Core.DTOs.Availability.Errors;
using Application.Core.DTOs.Diagnostic.Errors;
using Application.Core.DTOs.Auth.Errors;
using Application.Core.DTOs.Encryption.Errors;
using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Mfa.Errors;
using Application.Core.DTOs.Navigation.Errors;
using Application.Core.DTOs.Patient.Errors;
using Application.Core.DTOs.PatientAccess.Errors;
using Application.Core.DTOs.PatientAuth.Errors;
using Application.Core.DTOs.Person.Errors;
using Application.Core.DTOs.Prescription.Errors;
using Application.Core.DTOs.Clinic.Errors;
using Application.Core.DTOs.PartnerConfig.Errors;
using Application.Core.DTOs.Catalog.Errors;
using Application.Core.DTOs.Inventory.Errors;
using Application.Core.DTOs.PartnerCatalog.Errors;
using Application.Core.DTOs.Report.Errors;
using Application.Core.DTOs.Role.Errors;
using Application.Core.DTOs.Specialty.Errors;
using Application.Core.DTOs.User.Errors;
using RxLinkApi.Mappings;

namespace RxLinkApi.DependencyInjection;

internal static class ErrorMapperRegistration
{
    extension(WebApplicationBuilder builder)
    {
        internal void RegisterErrorMappers()
        {
            builder.Services.AddScoped<IErrorHttpMapper<AppointmentError>, AppointmentErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<AvailabilityError>, AvailabilityErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<PersonError>, PersonErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<PatientError>, PatientErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<PatientAccessError>, PatientAccessErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<AuthenticationError>, AuthErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<NavigationError>, NavigationErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<LookupError>, LookupErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<RoleError>, RoleErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<ReportError>, ReportErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<MfaError>, MfaErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<UserError>, UserErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<SpecialtyError>, SpecialtyErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<ClinicError>, ClinicErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<AmendmentError>, AmendmentErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<DisclosureError>, DisclosureErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<PrivacyNoticeError>, PrivacyNoticeErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<ConsentError>, ConsentErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<CommunicationPreferenceError>, CommunicationPreferenceErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<PartnerCatalogError>, PartnerCatalogErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<InventoryError>, InventoryErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<CatalogReadError>, CatalogReadErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<PartnerConfigError>, PartnerConfigErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<DiagnosticError>, DiagnosticErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<PrescriptionError>, PrescriptionErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<PatientAuthError>, PatientAuthErrorMapper>();
        }
    }
}