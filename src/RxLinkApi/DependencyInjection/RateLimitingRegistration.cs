using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace RxLinkApi.DependencyInjection;

internal static class RateLimitingRegistration
{
    /// <summary>Policy for credential endpoints (login, MFA, refresh), partitioned by client IP.</summary>
    internal const string AuthPolicy = "auth";

    /// <summary>Stricter policy for the unauthenticated patient lookup/registration (enumeration oracle).</summary>
    internal const string SensitiveAnonymousPolicy = "sensitive-anonymous";

    extension(WebApplicationBuilder builder)
    {
        /// <summary>
        /// Trusts <c>X-Forwarded-For</c> only from the configured proxies (default: loopback, i.e. the local nginx),
        /// so the client IP seen by the rate limiter and the audit trail is the real one, not the proxy's.
        /// </summary>
        internal void RegisterForwardedHeaders()
        {
            string[] knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies")
                .Get<string[]>() ?? [];

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                foreach (string proxy in knownProxies)
                    options.KnownProxies.Add(IPAddress.Parse(proxy));
            });
        }

        internal void RegisterRateLimiting()
        {
            int authPerMinute = builder.Configuration.GetValue("RateLimiting:AuthPermitPerMinute", 30);
            int sensitivePerMinute = builder.Configuration.GetValue("RateLimiting:SensitiveAnonymousPermitPerMinute", 10);

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(AuthPolicy, context => PerIp(context, authPerMinute));
                options.AddPolicy(SensitiveAnonymousPolicy, context => PerIp(context, sensitivePerMinute));

                options.OnRejected = async (context, token) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new { message = "Demasiadas solicitudes. Intente nuevamente en unos instantes." }, token);
                };
            });
        }
    }

    private static RateLimitPartition<string> PerIp(HttpContext context, int permitPerMinute) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
}
