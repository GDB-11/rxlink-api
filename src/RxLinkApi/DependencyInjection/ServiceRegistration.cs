using Application.Core.Interfaces.Auth;
using Application.Core.Interfaces.Shared;
using Application.Core.Services.Auth;
using Application.Core.Services.Shared;

namespace RxLinkApi.DependencyInjection;

internal static class ServiceRegistration
{
    extension(WebApplicationBuilder builder)
    {
        internal void RegisterApplicationServices()
        {
            builder.Services.AddScoped<IJwt, JwtService>();
            builder.Services.AddScoped<IPassword, PasswordService>();
            builder.Services.AddScoped<IChaChaEncryption, ChaChaEncryptionService>();
            builder.Services.AddScoped<IDeterministicEncryption, DeterministicAesEncryptionService>();
            builder.Services.AddScoped<IEncryption, EncryptionService>();
            builder.Services.AddScoped<ITimeProvider, SystemTimeProviderService>();
            builder.Services.AddScoped<IAuthentication, AuthenticationService>();
        }
    }
}