using Application.Core.Interfaces.Allergy;
using Application.Core.Interfaces.Appointment;
using Application.Core.Interfaces.Availability;
using Application.Core.Interfaces.Diagnostic;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.ConsultationType;
using Application.Core.Interfaces.DocumentType;
using Application.Core.Interfaces.Lookup;
using Application.Core.Interfaces.Medication;
using Application.Core.Interfaces.Navigation;
using Application.Core.Interfaces.Patient;
using Application.Core.Interfaces.PatientAuth;
using Application.Core.Interfaces.Person;
using Application.Core.Interfaces.Prescription;
using Application.Core.Interfaces.Role;
using Application.Core.Interfaces.Sex;
using Application.Core.Interfaces.Shared;
using Application.Core.Interfaces.Specialty;
using Application.Core.Interfaces.Users;
using Application.Core.Services.Allergy;
using Application.Core.Services.Appointment;
using Application.Core.Services.Availability;
using Application.Core.Services.Diagnostic;
using Application.Core.Services.Auth;
using Application.Core.Services.ConsultationType;
using Application.Core.Services.DocumentType;
using Application.Core.Services.Lookup;
using Application.Core.Services.Medication;
using Application.Core.Services.Navigation;
using Application.Core.Services.Patient;
using Application.Core.Services.PatientAuth;
using Application.Core.Services.Person;
using Application.Core.Services.Prescription;
using Application.Core.Services.Role;
using Application.Core.Services.Sex;
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
            builder.Services.AddScoped<IAllergy, AllergyService>();
            builder.Services.AddScoped<IAppointment, AppointmentService>();
            builder.Services.AddScoped<IAvailability, AvailabilityService>();
            builder.Services.AddScoped<IPerson, PersonService>();
            builder.Services.AddScoped<IPatient, PatientService>();
            builder.Services.AddScoped<IJwt, JwtService>();
            builder.Services.AddScoped<IPassword, PasswordService>();
            builder.Services.AddScoped<IChaChaEncryption, ChaChaEncryptionService>();
            builder.Services.AddScoped<IDeterministicEncryption, DeterministicAesEncryptionService>();
            builder.Services.AddScoped<IEncryption, EncryptionService>();
            builder.Services.AddScoped<ITimeProvider, SystemTimeProviderService>();
            builder.Services.AddScoped<IAuthentication, AuthenticationService>();
            builder.Services.AddScoped<INavigation, NavigationService>();
            builder.Services.AddScoped<IMedication, MedicationService>();
            builder.Services.AddScoped<ILookup, LookupService>();
            builder.Services.AddScoped<IRole, RoleService>();
            builder.Services.AddScoped<IUser, UserService>();
            builder.Services.AddScoped<ISpecialty, SpecialtyService>();
            builder.Services.AddScoped<IDiagnostic, DiagnosticService>();
            builder.Services.AddScoped<IPrescription, PrescriptionService>();
            builder.Services.AddScoped<IPatientAuthentication, PatientAuthenticationService>();
            builder.Services.AddScoped<ISex, SexService>();
            builder.Services.AddScoped<IDocumentType, DocumentTypeService>();
            builder.Services.AddScoped<IConsultationType, ConsultationTypeService>();
        }
    }
}