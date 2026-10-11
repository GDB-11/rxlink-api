using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace RxLinkApi.DependencyInjection;

internal static class AuthenticationRegistration
{
    extension(WebApplicationBuilder builder)
    {
        internal void RegisterAuthentication()
        {
            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            byte[] key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);

            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["Issuer"],
                        ValidAudience = jwtSettings["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = ctx =>
                        {
                            var log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                            log.LogWarning("[JWT] OnAuthenticationFailed — {ExceptionType}", ctx.Exception.GetType().Name);
                            return Task.CompletedTask;
                        },

                        OnChallenge = ctx =>
                        {
                            var log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                            log.LogWarning("[JWT] OnChallenge — error: {Error}", ctx.Error ?? "<none>");
                            return Task.CompletedTask;
                        }
                    };
                });
        }
    }
}