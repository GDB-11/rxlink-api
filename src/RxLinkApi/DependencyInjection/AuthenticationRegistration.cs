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
                        OnMessageReceived = ctx =>
                        {
                            var log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                            var header = ctx.Request.Headers.Authorization.FirstOrDefault();
                            log.LogDebug("[JWT] OnMessageReceived — Authorization header: {Header}",
                                string.IsNullOrEmpty(header)
                                    ? "<missing>"
                                    : header[..Math.Min(40, header.Length)] + "...");
                            return Task.CompletedTask;
                        },

                        OnTokenValidated = ctx =>
                        {
                            var log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                            var claims = ctx.Principal?.Claims.Select(c => $"{c.Type}={c.Value}");
                            log.LogDebug("[JWT] OnTokenValidated — claims: {Claims}", string.Join(", ", claims ?? []));
                            return Task.CompletedTask;
                        },

                        OnAuthenticationFailed = ctx =>
                        {
                            var log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                            log.LogWarning("[JWT] OnAuthenticationFailed — {ExceptionType}: {Message}",
                                ctx.Exception.GetType().Name, ctx.Exception.Message);
                            return Task.CompletedTask;
                        },

                        OnChallenge = ctx =>
                        {
                            var log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                            log.LogWarning("[JWT] OnChallenge — error: {Error}, description: {Description}",
                                ctx.Error ?? "<none>", ctx.ErrorDescription ?? "<none>");
                            return Task.CompletedTask;
                        }
                    };
                });
        }
    }
}