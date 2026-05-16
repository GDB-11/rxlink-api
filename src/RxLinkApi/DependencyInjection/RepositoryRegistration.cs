using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Services.Account;

namespace RxLinkApi.DependencyInjection;

internal static class RepositoryRegistration
{
    extension(WebApplicationBuilder builder)
    {
        internal void RegisterRepositories()
        {
            builder.Services.AddScoped<ICredentialRepository, CredentialRepository>();
        }
    }
}