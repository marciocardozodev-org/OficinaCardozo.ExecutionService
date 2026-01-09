using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using OficinaCardozo.Infrastructure.Data;
using System;
using System.IO;

namespace OficinaCardozo.Infrastructure.Factories
{
    /// <summary>
    /// F�brica para criar inst�ncias de OficinaDbContext em tempo de design (ex: para criar migra��es).
    /// </summary>
    public class OficinaDbContextFactory : IDesignTimeDbContextFactory<OficinaDbContext>
    {
        public OficinaDbContext CreateDbContext(string[] args)
        {
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            string apiProjectPath = Environment.GetEnvironmentVariable("API_PROJECT_PATH")
                ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "OficinaCardozo.API");
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(apiProjectPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddUserSecrets<OficinaDbContextFactory>() 
                .AddEnvironmentVariables()
                .Build();
            var optionsBuilder = new DbContextOptionsBuilder<OficinaDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            Console.WriteLine($"[OficinaDbContextFactory] ENV: {environment}");
            Console.WriteLine($"[OficinaDbContextFactory] API_PROJECT_PATH: {apiProjectPath}");
            Console.WriteLine($"[OficinaDbContextFactory] ConnectionString recebida: {connectionString}");
            Console.WriteLine($"[OficinaDbContextFactory] Teste Host= na connection string: {(!string.IsNullOrEmpty(connectionString) && (connectionString.Contains("Host=") || connectionString.Contains("host=")))}");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("A string de conexão 'DefaultConnection' não foi encontrada.");
            }
            if (!string.IsNullOrEmpty(connectionString) && (connectionString.Contains("Host=") || connectionString.Contains("host=")))
            {
                Console.WriteLine("[OficinaDbContextFactory] PROVIDER: PostgreSQL");
                optionsBuilder.UseNpgsql(connectionString,
                    npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(OficinaDbContext).Assembly.FullName));
            }
            else if (environment == "Development")
            {
                Console.WriteLine("[OficinaDbContextFactory] PROVIDER: SQL Server");
                var userId = configuration["DatabaseCredentials:UserId"];
                var password = configuration["DatabaseCredentials:Password"];
                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(password))
                {
                    var csBuilder = new SqlConnectionStringBuilder(connectionString);
                    csBuilder.UserID = userId;
                    csBuilder.Password = password;
                    connectionString = csBuilder.ConnectionString;
                }
                optionsBuilder.UseSqlServer(connectionString,
                    sqlOptions => sqlOptions.MigrationsAssembly(typeof(OficinaDbContext).Assembly.FullName));
            }
            else
            {
                Console.WriteLine("[OficinaDbContextFactory] PROVIDER: SQLite");
                optionsBuilder.UseSqlite(connectionString,
                    sqliteOptions => sqliteOptions.MigrationsAssembly(typeof(OficinaDbContext).Assembly.FullName));
            }
            return new OficinaDbContext(optionsBuilder.Options);
        }
    }
}