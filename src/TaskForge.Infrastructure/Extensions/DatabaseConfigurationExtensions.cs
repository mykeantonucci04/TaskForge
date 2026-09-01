using Microsoft.Extensions.Configuration;
using Npgsql;

namespace TaskForge.Infrastructure.Extensions;

public static class DatabaseConfigurationExtensions
{
    public static (string ConnectionString, string Source) ResolveDatabaseConnectionString(this IConfiguration configuration)
    {
        // 1. Check for Render / Neon DATABASE_URL environment variable
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return (ConvertPostgresUriToNpgsqlConnectionString(databaseUrl), "DATABASE_URL (Environment Variable)");
        }

        // 2. Check for ConnectionStrings:DefaultConnection (or ConnectionStrings__DefaultConnection)
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return (ConvertPostgresUriToNpgsqlConnectionString(connectionString), "ConnectionStrings:DefaultConnection (Postgres URI)");
            }

            return (connectionString, "ConnectionStrings:DefaultConnection (Npgsql Format)");
        }

        return (string.Empty, "None");
    }

    public static string ConvertPostgresUriToNpgsqlConnectionString(string uriString)
    {
        var uri = new Uri(uriString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        var npgsqlBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = database,
            Username = username,
            Password = password,
            SslMode = SslMode.Require,
            TrustServerCertificate = true
        };

        return npgsqlBuilder.ConnectionString;
    }
}
