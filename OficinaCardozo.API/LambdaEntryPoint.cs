using System.Text;
using Amazon.Lambda.AspNetCoreServer;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OficinaCardozo.Application.DTOs;
using OficinaCardozo.Application.Interfaces;
using OficinaCardozo.Application.Mappers;
using OficinaCardozo.Application.Services;
using OficinaCardozo.Application.Settings;
using OficinaCardozo.Domain.Interfaces.Repositories;
using OficinaCardozo.Domain.Interfaces.Services;
using OficinaCardozo.Infrastructure.Data;
using OficinaCardozo.Infrastructure.Repositories;

// Serializador necessário para Lambda
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace OficinaCardozo.API;

/// <summary>
/// Lambda Entry Point - APIGatewayHttpApiV2ProxyFunction
/// Configura DI, banco de dados, autenticação e Swagger para rodar na AWS Lambda.
/// </summary>
public class LambdaEntryPoint : APIGatewayHttpApiV2ProxyFunction
{
    protected override void Init(IWebHostBuilder builder)
    {
        builder
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                // Carregar variáveis de ambiente e, em produção, secrets por arquivo
                config.AddEnvironmentVariables();

                if (hostingContext.HostingEnvironment.IsProduction())
                {
                    config.AddKeyPerFile(directoryPath: "/run/secrets", optional: true);
                }
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                var environment = context.HostingEnvironment;

                Console.WriteLine("Iniciando configuração da API Oficina Cardozo (LambdaEntryPoint)...");

                services.AddControllers();
                services.AddEndpointsApiExplorer();

                // Configurações fortemente tipadas
                services.Configure<ConfiguracoesJwt>(configuration.GetSection("ConfiguracoesJwt"));
                services.Configure<ConfiguracoesEmail>(configuration.GetSection("ConfiguracoesEmail"));

                // Swagger com definição de segurança Bearer
                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "Oficina Cardozo API",
                        Version = "v1",
                        Description = "API para gerenciamento da Oficina Cardozo (Lambda)"
                    });

                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Authorization: Bearer {token}\"",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                },
                                Scheme = "oauth2",
                                Name = "Bearer",
                                In = ParameterLocation.Header
                            },
                            new List<string>()
                        }
                    });
                });

                // Banco de dados (PostgreSQL em produção / SQLite em dev)
                Console.WriteLine($"🌍 Ambiente: {environment.EnvironmentName}");

                // 1) TENTATIVA PRINCIPAL (LAMBDA): montar a connection string a partir de Db:Host/Name/User/Password
                // As variáveis vêm do Terraform como Db__Host/Db__Name/Db__User/Db__Password
                var dbHostFromEnv = configuration["Db:Host"];
                var dbNameFromEnv = configuration["Db:Name"] ?? "oficinacardozo";
                var dbUserFromEnv = configuration["Db:User"] ?? "dbadmin";
                var dbPasswordFromEnv = configuration["Db:Password"];

                string? connectionString;

                if (!string.IsNullOrEmpty(dbHostFromEnv) && !string.IsNullOrEmpty(dbPasswordFromEnv))
                {
                    Console.WriteLine("🔧 Construindo connection string PostgreSQL a partir de Db:Host/Name/User/Password (ignorando ConnectionStrings:DefaultConnection se existir)...");
                    var builder = new Npgsql.NpgsqlConnectionStringBuilder
                    {
                        Host = dbHostFromEnv,
                        Database = dbNameFromEnv,
                        Username = dbUserFromEnv,
                        Password = dbPasswordFromEnv
                    };
                    connectionString = builder.ConnectionString;
                }
                else
                {
                    // 2) FALLBACK (ambiente local / testes): usar ConnectionStrings:DefaultConnection
                    connectionString = configuration.GetConnectionString("DefaultConnection");
                }

                Console.WriteLine($"🔍 Connection String efetiva: {(string.IsNullOrEmpty(connectionString) ? "NULL/VAZIA" : connectionString.Substring(0, Math.Min(80, connectionString.Length)))}...");

                services.AddDbContext<OficinaDbContext>(options =>
                {
                    if (connectionString != null)
                    {
                        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("✅ Configurando o provedor de banco de dados para PostgreSQL (Lambda).");
                            options.UseNpgsql(connectionString,
                                npgsqlOptions => npgsqlOptions.MigrationsAssembly(typeof(OficinaDbContext).Assembly.FullName));
                        }
                        else
                        {
                            Console.WriteLine("✅ Configurando o provedor de banco de dados para SQLite (ambiente local).");
                            var dbPath = connectionString.Contains("Data Source=") ? connectionString.Split('=')[1] : connectionString;
                            var dbFolder = Path.GetDirectoryName(dbPath);
                            if (!string.IsNullOrEmpty(dbFolder) && !Directory.Exists(dbFolder))
                            {
                                Console.WriteLine($"📁 Criando diretório para o banco de dados SQLite em: {dbFolder}");
                                Directory.CreateDirectory(dbFolder);
                            }

                            var sqliteConnectionString = connectionString.Contains("Data Source=")
                                ? connectionString
                                : $"Data Source={connectionString}";

                            options.UseSqlite(sqliteConnectionString,
                                sqliteOptions => sqliteOptions.MigrationsAssembly(typeof(OficinaDbContext).Assembly.FullName));
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ ERRO: Connection string 'DefaultConnection' não encontrada!");
                        throw new InvalidOperationException("A string de conexão 'DefaultConnection' não foi encontrada.");
                    }

                    if (environment.IsDevelopment())
                    {
                        options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
                        options.EnableDetailedErrors();
                        options.EnableSensitiveDataLogging();
                    }
                });

                // Autenticação JWT
                var jwtKey = configuration["ConfiguracoesJwt:ChaveSecreta"];
                if (string.IsNullOrEmpty(jwtKey))
                {
                    throw new InvalidOperationException("JWT Key não foi configurada. Verifique as variáveis de ambiente ConfiguracoesJwt__ChaveSecreta.");
                }

                var key = Encoding.ASCII.GetBytes(jwtKey);

                services.AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(x =>
                {
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

                services.AddAuthorization(options =>
                {
                    options.AddPolicy("RequireCpf", policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.RequireClaim("cpfCnpj");
                    });
                });

                // Repositórios e serviços da aplicação
                services.AddScoped<IUsuarioRepository, UsuarioRepository>();
                services.AddScoped<IClienteRepository, ClienteRepository>();
                services.AddScoped<IVeiculoRepository, VeiculoRepository>();
                services.AddScoped<IServicoRepository, ServicoRepository>();
                services.AddScoped<IPecaRepository, PecaRepository>();
                services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();
                services.AddScoped<IOrcamentoRepository, OrcamentoRepository>();
                services.AddScoped<IOrdemServicoStatusRepository, OrdemServicoStatusRepository>();
                services.AddScoped<IOrcamentoStatusRepository, OrcamentoStatusRepository>();

                services.AddScoped<IClienteMapper, ClienteMapper>();
                services.AddScoped<IVeiculoMapper, VeiculoMapper>();
                services.AddScoped<IServicoMapper, ServicoMapper>();

                services.AddScoped<IAutenticacaoService, AutenticacaoService>();
                services.AddScoped<IClienteService, ClienteService>();
                services.AddScoped<IVeiculoService, VeiculoService>();
                services.AddScoped<IServicoService, ServicoService>();
                services.AddScoped<IPecaService, PecaService>();
                services.AddScoped<IOrdemServicoService, OrdemServicoService>();
                services.AddScoped<ICpfCnpjValidationService, CpfCnpjValidationService>();

                services.AddScoped<IOrdemServicoStatusService, OrdemServicoStatusService>();
                services.AddScoped<IEmailMonitorService, EmailMonitorService>();

                services.AddHostedService<EmailMonitorService>();

                services.AddCors(options =>
                {
                    options.AddPolicy("AllowAll", policy =>
                    {
                        policy
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
                });
            })
            .Configure(app =>
            {
                var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                var isLambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));

                Console.WriteLine($"📋 Configurando pipeline HTTP. Ambiente: {env.EnvironmentName}. Lambda: {isLambda}");

                                // Garantir que o schema do banco esteja criado/aplicado e ajustado (especialmente no Postgres do RDS)
                                try
                                {
                                        using var scope = app.ApplicationServices.CreateScope();
                                        var dbContext = scope.ServiceProvider.GetRequiredService<OficinaDbContext>();

                                        Console.WriteLine("📦 Verificando/aplicando migrações pendentes do banco de dados...");
                                        dbContext.Database.Migrate();
                                        Console.WriteLine("✅ Migrações aplicadas (ou já estavam em dia).");

                                        // Ajustes adicionais específicos para Postgres (identidade das PKs etc.)
                                        var providerName = dbContext.Database.ProviderName;
                                        if (string.Equals(providerName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.OrdinalIgnoreCase))
                                        {
                                                Console.WriteLine("🔧 Aplicando ajustes de identidade nas tabelas principais (Postgres)...");

                                                var sql = @"DO $$
BEGIN
    -- Garante identity nas PKs principais
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'OFICINA_USUARIO' AND column_name = 'ID' AND is_identity = 'YES') THEN
        ALTER TABLE ""OFICINA_USUARIO"" ALTER COLUMN ""ID"" ADD GENERATED BY DEFAULT AS IDENTITY;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'OFICINA_CLIENTE' AND column_name = 'ID' AND is_identity = 'YES') THEN
        ALTER TABLE ""OFICINA_CLIENTE"" ALTER COLUMN ""ID"" ADD GENERATED BY DEFAULT AS IDENTITY;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'OFICINA_VEICULO' AND column_name = 'ID' AND is_identity = 'YES') THEN
        ALTER TABLE ""OFICINA_VEICULO"" ALTER COLUMN ""ID"" ADD GENERATED BY DEFAULT AS IDENTITY;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'OFICINA_PECA' AND column_name = 'ID' AND is_identity = 'YES') THEN
        ALTER TABLE ""OFICINA_PECA"" ALTER COLUMN ""ID"" ADD GENERATED BY DEFAULT AS IDENTITY;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'OFICINA_SERVICO' AND column_name = 'ID' AND is_identity = 'YES') THEN
        ALTER TABLE ""OFICINA_SERVICO"" ALTER COLUMN ""ID"" ADD GENERATED BY DEFAULT AS IDENTITY;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'OFICINA_ORDEM_SERVICO_STATUS' AND column_name = 'ID' AND is_identity = 'YES') THEN
        ALTER TABLE ""OFICINA_ORDEM_SERVICO_STATUS"" ALTER COLUMN ""ID"" ADD GENERATED BY DEFAULT AS IDENTITY;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'OFICINA_ORDEM_SERVICO' AND column_name = 'ID' AND is_identity = 'YES') THEN
        ALTER TABLE ""OFICINA_ORDEM_SERVICO"" ALTER COLUMN ""ID"" ADD GENERATED BY DEFAULT AS IDENTITY;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'OFICINA_ORCAMENTO_STATUS' AND column_name = 'ID' AND is_identity = 'YES') THEN
        ALTER TABLE ""OFICINA_ORCAMENTO_STATUS"" ALTER COLUMN ""ID"" ADD GENERATED BY DEFAULT AS IDENTITY;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'OFICINA_ORCAMENTO' AND column_name = 'ID' AND is_identity = 'YES') THEN
        ALTER TABLE ""OFICINA_ORCAMENTO"" ALTER COLUMN ""ID"" ADD GENERATED BY DEFAULT AS IDENTITY;
    END IF;

    -- Ajusta tipos das colunas decimais principais para numeric(18,2) quando ainda forem text
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'OFICINA_SERVICO' AND column_name = 'PRECO' AND data_type = 'text') THEN
        ALTER TABLE ""OFICINA_SERVICO""
        ALTER COLUMN ""PRECO"" TYPE numeric(18,2)
        USING NULLIF(""PRECO"", '')::numeric(18,2);
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'OFICINA_PECA' AND column_name = 'PRECO' AND data_type = 'text') THEN
        ALTER TABLE ""OFICINA_PECA""
        ALTER COLUMN ""PRECO"" TYPE numeric(18,2)
        USING NULLIF(""PRECO"", '')::numeric(18,2);
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'OFICINA_ORDEM_SERVICO_SERVICO' AND column_name = 'VALOR_APLICADO' AND data_type = 'text') THEN
        ALTER TABLE ""OFICINA_ORDEM_SERVICO_SERVICO""
        ALTER COLUMN ""VALOR_APLICADO"" TYPE numeric(18,2)
        USING NULLIF(""VALOR_APLICADO"", '')::numeric(18,2);
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'OFICINA_ORDEM_SERVICO_PECA' AND column_name = 'VALOR_UNITARIO' AND data_type = 'text') THEN
        ALTER TABLE ""OFICINA_ORDEM_SERVICO_PECA""
        ALTER COLUMN ""VALOR_UNITARIO"" TYPE numeric(18,2)
        USING NULLIF(""VALOR_UNITARIO"", '')::numeric(18,2);
    END IF;
END
$$;";

                                                dbContext.Database.ExecuteSqlRaw(sql);
                                                Console.WriteLine("✅ Ajustes de identidade aplicados (ou já estavam corretos).");
                                        }
                                }
                                catch (Exception ex)
                                {
                                        Console.WriteLine($"❌ Erro ao aplicar migrações/ajustes do banco: {ex.Message}");
                                        throw;
                                }

                // Swagger na raiz
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Oficina Cardozo API v1");
                    c.RoutePrefix = string.Empty;
                    c.DocumentTitle = "Oficina Cardozo API - Swagger UI";
                });

                // Logging de requisições em ambiente Lambda
                if (isLambda)
                {
                    app.Use(async (context, next) =>
                    {
                        var path = context.Request.Path.Value ?? "/";
                        var method = context.Request.Method;
                        Console.WriteLine($"🔍 [{method}] {path}");

                        await next();

                        var statusCode = context.Response.StatusCode;
                        var statusEmoji = statusCode >= 200 && statusCode < 300 ? "✅" :
                                         statusCode >= 400 && statusCode < 500 ? "⚠️" : "❌";
                        Console.WriteLine($"{statusEmoji} [{method}] {path} → {statusCode}");
                    });
                }

                app.UseCors("AllowAll");

                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

                Console.WriteLine("✅ Pipeline HTTP configurado (LambdaEntryPoint).");
            });
    }
}
