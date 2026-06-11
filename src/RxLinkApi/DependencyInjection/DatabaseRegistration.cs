using System.Data;
using Npgsql;

namespace RxLinkApi.DependencyInjection;

internal static class DatabaseRegistration
{
    extension(WebApplicationBuilder builder)
    {
        internal void RegisterDatabase()
        {
            builder.Services.AddTransient<IDbConnection>(sp =>
                new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
        }
    }
}