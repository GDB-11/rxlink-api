using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using RxLinkApi.DependencyInjection;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.RegisterKeys();

builder.RegisterDatabase();

builder.RegisterCatalog();

builder.RegisterLogger();

builder.RegisterRepositories();

builder.RegisterErrorMappers();

builder.RegisterApplicationServices();

builder.Services.AddHostedService<RxLinkApi.DependencyInjection.PrescriptionExpiryService>();

// Channel providers are chosen per deployment: Notifications:<Channel>:Provider = None (default, off) | Log (accepts and logs only the channel).
foreach (string channel in new[] { Infrastructure.Core.Interfaces.Notifications.NotificationChannels.Email, Infrastructure.Core.Interfaces.Notifications.NotificationChannels.Sms })
{
    if (string.Equals(builder.Configuration[$"Notifications:{channel}:Provider"], "Log", StringComparison.OrdinalIgnoreCase))
        builder.Services.AddSingleton<Infrastructure.Core.Interfaces.Notifications.INotificationChannel>(sp =>
            new RxLinkApi.DependencyInjection.LogOnlyNotificationChannel(channel,
                sp.GetRequiredService<ILogger<RxLinkApi.DependencyInjection.LogOnlyNotificationChannel>>()));
}
builder.Services.AddHostedService<RxLinkApi.DependencyInjection.NotificationDispatchService>();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            Dictionary<string, string[]> errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => char.ToLowerInvariant(kvp.Key[0]) + kvp.Key[1..],
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return new BadRequestObjectResult(new
            {
                title = "One or more validation errors occurred.",
                errors
            });
        };
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddPatientContractOpenApi();

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MemoryBufferThreshold = int.MaxValue;
});

builder.RegisterAuthentication();

builder.RegisterForwardedHeaders();

builder.RegisterRateLimiting();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    options.AddPolicy("ProductionCors", policy =>
    {
        string[] allowedOrigins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>() ?? [];

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Must run first so every later component (rate limiter, audit IP) sees the real client address.
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("API de RxLink - Sistema para clínicas")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .AddPreferredSecuritySchemes();
    });
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(app.Environment.IsDevelopment() ? "DevelopmentCors" : "ProductionCors");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Anonymous, status text only (no check descriptions or data in the body).
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();