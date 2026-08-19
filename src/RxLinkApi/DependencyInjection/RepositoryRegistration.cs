using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Interfaces.Allergy;
using Infrastructure.Core.Interfaces.Appointment;
using Infrastructure.Core.Interfaces.Availability;
using Infrastructure.Core.Interfaces.Diagnostic;
using Infrastructure.Core.Interfaces.Lookup;
using Infrastructure.Core.Interfaces.Medication;
using Infrastructure.Core.Interfaces.Navigation;
using Infrastructure.Core.Interfaces.Patient;
using Infrastructure.Core.Interfaces.PatientAuth;
using Infrastructure.Core.Interfaces.Person;
using Infrastructure.Core.Interfaces.Prescription;
using Infrastructure.Core.Interfaces.Insurance;
using Infrastructure.Core.Interfaces.Role;
using Infrastructure.Core.Interfaces.Specialty;
using Infrastructure.Core.Interfaces.Users;
using Infrastructure.Core.Services.Account;
using Infrastructure.Core.Services.Allergy;
using Infrastructure.Core.Services.Appointment;
using Infrastructure.Core.Services.Availability;
using Infrastructure.Core.Services.Diagnostic;
using Infrastructure.Core.Services.Lookup;
using Infrastructure.Core.Services.Medication;
using Infrastructure.Core.Services.Navigation;
using Infrastructure.Core.Services.Patient;
using Infrastructure.Core.Services.PatientAuth;
using Infrastructure.Core.Services.Person;
using Infrastructure.Core.Services.Prescription;
using Infrastructure.Core.Services.Insurance;
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
            builder.Services.AddScoped<ICredentialRepository, CredentialRepository>();
            builder.Services.AddScoped<IAllergyRepository, AllergyRepository>();
            builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
            builder.Services.AddScoped<IAvailabilityRepository, AvailabilityRepository>();
            builder.Services.AddScoped<IPersonRepository, PersonRepository>();
            builder.Services.AddScoped<IPatientRepository, PatientRepository>();
            builder.Services.AddScoped<INavigationRepository, NavigationRepository>();
            builder.Services.AddScoped<IMedicationRepository, MedicationRepository>();
            builder.Services.AddScoped<ILookupRepository, LookupRepository>();
            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ISpecialtyRepository, SpecialtyRepository>();
            builder.Services.AddScoped<IInsuranceRepository, InsuranceRepository>();
            builder.Services.AddScoped<IDiagnosticRepository, DiagnosticRepository>();
            builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
            builder.Services.AddScoped<IPatientCredentialRepository, PatientCredentialRepository>();
        }
    }
}