using Infrastructure.Core.Interfaces.Activity;
using Infrastructure.Core.Interfaces.Audit;
using Infrastructure.Core.Services.Activity;
using RxLinkApi.Http;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Interfaces.Appointment;
using Infrastructure.Core.Interfaces.Availability;
using Infrastructure.Core.Interfaces.Diagnostic;
using Infrastructure.Core.Interfaces.Lookup;
using Infrastructure.Core.Interfaces.Mfa;
using Infrastructure.Core.Interfaces.Navigation;
using Infrastructure.Core.Interfaces.Patient;
using Infrastructure.Core.Interfaces.PatientAccess;
using Infrastructure.Core.Interfaces.PatientAuth;
using Infrastructure.Core.Interfaces.Person;
using Infrastructure.Core.Interfaces.Prescription;
using Infrastructure.Core.Interfaces.Catalog;
using Infrastructure.Core.Interfaces.Clinic;
using Infrastructure.Core.Interfaces.PartnerCatalog;
using Infrastructure.Core.Interfaces.Report;
using Infrastructure.Core.Interfaces.Role;
using Infrastructure.Core.Interfaces.Specialty;
using Infrastructure.Core.Interfaces.Users;
using Infrastructure.Core.Services.Account;
using Infrastructure.Core.Services.Appointment;
using Infrastructure.Core.Services.Availability;
using Infrastructure.Core.Services.Diagnostic;
using Infrastructure.Core.Services.Lookup;
using Infrastructure.Core.Services.Mfa;
using Infrastructure.Core.Services.Navigation;
using Infrastructure.Core.Services.Patient;
using Infrastructure.Core.Services.PatientAccess;
using Infrastructure.Core.Services.PatientAuth;
using Infrastructure.Core.Services.Person;
using Infrastructure.Core.Services.Prescription;
using Infrastructure.Core.Services.Catalog;
using Infrastructure.Core.Services.Clinic;
using Infrastructure.Core.Services.PartnerCatalog;
using Infrastructure.Core.Services.Report;
using Infrastructure.Core.Services.Role;
using Infrastructure.Core.Services.Specialty;
using Infrastructure.Core.Services.Users;

namespace RxLinkApi.DependencyInjection;

internal static class RepositoryRegistration
{
    extension(WebApplicationBuilder builder)
    {
        internal void RegisterRepositories()
        {
            builder.Services.AddScoped<IUserContext, HttpUserContext>();
            builder.Services.AddScoped<IActivityLogger, ActivityLogger>();
            builder.Services.AddScoped<IRecordAccessLogger, RecordAccessLogger>();
            builder.Services.AddScoped<ICredentialRepository, CredentialRepository>();
            builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            builder.Services.AddScoped<IAvailabilityRepository, AvailabilityRepository>();
            builder.Services.AddScoped<IPersonRepository, PersonRepository>();
            builder.Services.AddScoped<IPatientRepository, PatientRepository>();
            builder.Services.AddScoped<IPatientAccessRepository, PatientAccessRepository>();
            builder.Services.AddScoped<INavigationRepository, NavigationRepository>();
            builder.Services.AddScoped<ILookupRepository, LookupRepository>();
            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IReportRepository, ReportRepository>();
            builder.Services.AddScoped<IMfaRepository, MfaRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ISpecialtyRepository, SpecialtyRepository>();
            builder.Services.AddScoped<IClinicRepository, ClinicRepository>();
            builder.Services.AddScoped<IPartnerCatalogRepository, PartnerCatalogRepository>();
            builder.Services.AddScoped<IPartnerEnablementRepository, PartnerEnablementRepository>();
            builder.Services.AddScoped<IDiagnosticRepository, DiagnosticRepository>();
            builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
            builder.Services.AddScoped<IPatientCredentialRepository, PatientCredentialRepository>();
        }
    }
}