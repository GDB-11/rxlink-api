using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Interfaces.Allergy;
using Infrastructure.Core.Interfaces.Lookup;
using Infrastructure.Core.Interfaces.Medication;
using Infrastructure.Core.Interfaces.Navigation;
using Infrastructure.Core.Interfaces.Specialty;
using Infrastructure.Core.Interfaces.Users;
using Infrastructure.Core.Services.Account;
using Infrastructure.Core.Services.Allergy;
using Infrastructure.Core.Services.Lookup;
using Infrastructure.Core.Services.Medication;
using Infrastructure.Core.Services.Navigation;
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
            builder.Services.AddScoped<INavigationRepository, NavigationRepository>();
            builder.Services.AddScoped<IMedicationRepository, MedicationRepository>();
            builder.Services.AddScoped<ILookupRepository, LookupRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ISpecialtyRepository, SpecialtyRepository>();
        }
    }
}