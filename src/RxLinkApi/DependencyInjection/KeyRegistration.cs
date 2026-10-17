using Application.Core.Config;

namespace RxLinkApi.DependencyInjection;

internal static class KeyRegistration
{
    /// <summary>Secrets are never committed (Phase 1.1/9.14): an empty value means the environment variable was not set.</summary>
    private static string RequiredSecret(this IConfiguration configuration, string key) =>
        string.IsNullOrWhiteSpace(configuration[key])
            ? throw new InvalidOperationException(
                $"Configuration '{key}' is not set. Provide it as the environment variable '{key.Replace(":", "__")}' (see rxlink-sv/scripts/local-setup/README.md).")
            : configuration[key]!;

    extension(WebApplicationBuilder builder)
    {
        private void AddEncryptionMasterKey()
        {
            EncryptionConfig encryptionConfig = new()
            {
                MasterKey = builder.Configuration.RequiredSecret("Encryption:MasterKey")
            };
            builder.Services.AddSingleton(encryptionConfig);
        }

        private void AddDeterministicEncryptionKeys()
        {
            DeterministicEncryptionConfig deterministicEncryptionConfig = new()
            {
                MasterKey = builder.Configuration.RequiredSecret("DeterministicEncryption:MasterKey"),
                IvGenerationKey = builder.Configuration.RequiredSecret("DeterministicEncryption:IvGenerationKey")
            };
            builder.Services.AddSingleton(deterministicEncryptionConfig);
        }

        private void AddJwtKeys()
        {
            JwtConfig jwtConfig = new()
            {
                SecretKey = builder.Configuration.RequiredSecret("JwtSettings:SecretKey"),
                Issuer = builder.Configuration["JwtSettings:Issuer"] ?? throw new NullReferenceException("Issuer"),
                Audience =
                    builder.Configuration["JwtSettings:Audience"] ?? throw new NullReferenceException("Audience"),
                AccessTokenExpiryMinutes = int.Parse(builder.Configuration["JwtSettings:AccessTokenExpiryMinutes"] ??
                                                     throw new NullReferenceException("AccessTokenExpiryMinutes")),
                RefreshTokenExpiryMinutes = int.Parse(builder.Configuration["JwtSettings:RefreshTokenExpiryMinutes"] ??
                                                      throw new NullReferenceException("RefreshTokenExpiryMinutes"))
            };
            builder.Services.AddSingleton(jwtConfig);
        }

        private void AddSessionSecurity()
        {
            SessionSecurityConfig defaults = new();
            IConfigurationSection section = builder.Configuration.GetSection("SessionSecurity");
            builder.Services.AddSingleton(new SessionSecurityConfig
            {
                MaxFailedAttempts = section.GetValue("MaxFailedAttempts", defaults.MaxFailedAttempts),
                LockoutMinutes = section.GetValue("LockoutMinutes", defaults.LockoutMinutes),
                AbsoluteSessionMinutes = section.GetValue("AbsoluteSessionMinutes", defaults.AbsoluteSessionMinutes)
            });
        }

        private void AddPasswordPolicy()
        {
            PasswordPolicyConfig defaults = new();
            IConfigurationSection section = builder.Configuration.GetSection("PasswordPolicy");
            builder.Services.AddSingleton(new PasswordPolicyConfig
            {
                MinLength = section.GetValue("MinLength", defaults.MinLength),
                MaxLength = section.GetValue("MaxLength", defaults.MaxLength),
                BreachCheckEnabled = section.GetValue("BreachCheckEnabled", defaults.BreachCheckEnabled)
            });
        }

        internal void RegisterKeys()
        {
            builder.AddPasswordPolicy();
            builder.AddSessionSecurity();
            builder.AddEncryptionMasterKey();
            builder.AddDeterministicEncryptionKeys();
            builder.AddJwtKeys();
        }
    }
}