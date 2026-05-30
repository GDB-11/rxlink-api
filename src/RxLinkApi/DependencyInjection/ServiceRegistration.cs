using Application.Core.Interfaces.Allergy;
using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Lookup;
using Application.Core.Interfaces.Medication;
using Application.Core.Interfaces.Navigation;
using Application.Core.Interfaces.Patient;
using Application.Core.Interfaces.Person;
using Application.Core.Interfaces.Shared;
using Application.Core.Interfaces.Specialty;
using Application.Core.Interfaces.Users;
using Application.Core.Services.Allergy;
using Application.Core.Services.Auth;
using Application.Core.Services.Lookup;
using Application.Core.Services.Medication;
using Application.Core.Services.Navigation;
using Application.Core.Services.Patient;
using Application.Core.Services.Person;
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
            builder.Services.AddScoped<IUser, UserService>();
            builder.Services.AddScoped<ISpecialty, SpecialtyService>();
        }
    }
}