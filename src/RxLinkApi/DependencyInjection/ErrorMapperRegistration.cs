using Application.Core.DTOs.Allergy.Errors;
using Application.Core.DTOs.Auth.Errors;
using Application.Core.DTOs.Encryption.Errors;
using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Medication.Errors;
using Application.Core.DTOs.Navigation.Errors;
using Application.Core.DTOs.Patient.Errors;
using Application.Core.DTOs.Person.Errors;
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
            builder.Services.AddScoped<IErrorHttpMapper<AllergyError>, AllergyErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<PersonError>, PersonErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<PatientError>, PatientErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<ChaChaEncryptionError>, ChaChaEncryptionErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<AuthenticationError>, AuthErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<NavigationError>, NavigationErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<MedicationError>, MedicationErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<LookupError>, LookupErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<UserError>, UserErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<SpecialtyError>, SpecialtyErrorMapper>();
        }
    }
}