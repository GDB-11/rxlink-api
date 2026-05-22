using Application.Core.DTOs.Auth.Errors;
using Application.Core.DTOs.Encryption.Errors;
using Application.Core.DTOs.Lookup.Errors;
using Application.Core.DTOs.Medication.Errors;
using Application.Core.DTOs.Navigation.Errors;
using RxLinkApi.Mappings;

namespace RxLinkApi.DependencyInjection;

internal static class ErrorMapperRegistration
{
    extension(WebApplicationBuilder builder)
    {
        internal void RegisterErrorMappers()
        {
            builder.Services.AddScoped<IErrorHttpMapper<ChaChaEncryptionError>, ChaChaEncryptionErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<AuthenticationError>, AuthErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<NavigationError>, NavigationErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<MedicationError>, MedicationErrorMapper>();
            builder.Services.AddScoped<IErrorHttpMapper<LookupError>, LookupErrorMapper>();
        }
    }
}