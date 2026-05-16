using Application.Core.DTOs.Auth.Errors;
using Application.Core.DTOs.Encryption.Errors;
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
        }
    }
}