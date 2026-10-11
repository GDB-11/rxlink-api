using Application.Core.Config;

namespace RxLinkApi.DependencyInjection;

internal static class KeyRegistration
{
    extension(WebApplicationBuilder builder)
    {
        private void AddEncryptionMasterKey()
        {
            EncryptionConfig encryptionConfig = new()
            {
                MasterKey = builder.Configuration["Encryption:MasterKey"] ??
                            throw new NullReferenceException("MasterKey")
            };
            builder.Services.AddSingleton(encryptionConfig);
        }

        private void AddDeterministicEncryptionKeys()
        {
            DeterministicEncryptionConfig deterministicEncryptionConfig = new()
            {
                MasterKey = builder.Configuration["DeterministicEncryption:MasterKey"] ??
                            throw new NullReferenceException("MasterKey"),
                IvGenerationKey = builder.Configuration["DeterministicEncryption:IvGenerationKey"] ??
                                  throw new NullReferenceException("IvGenerationKey")
            };
            builder.Services.AddSingleton(deterministicEncryptionConfig);
        }

        private void AddJwtKeys()
        {
            JwtConfig jwtConfig = new()
            {
                SecretKey = builder.Configuration["JwtSettings:SecretKey"] ??
                            throw new NullReferenceException("SecretKey"),
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