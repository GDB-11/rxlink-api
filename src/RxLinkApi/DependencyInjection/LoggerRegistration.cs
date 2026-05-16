using RxLinkApi.Logging;

namespace RxLinkApi.DependencyInjection;

internal static class LoggerRegistration
{
    extension(WebApplicationBuilder builder)
    {
        internal void RegisterLogger()
        {
            builder.Services.AddSingleton<IResultLogger, ConsoleResultLogger>();
        }
    }
}