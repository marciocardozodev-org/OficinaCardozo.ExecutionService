using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Data.SqlClient;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using OficinaCardozo.Application.Interfaces;
using OficinaCardozo.Application.Mappers;
using OficinaCardozo.Application.Services;
using OficinaCardozo.Application.Settings;
using OficinaCardozo.Domain.Interfaces;
using OficinaCardozo.Infrastructure.Data;
using OficinaCardozo.Infrastructure.Repositories;
using System.Text;
using Serilog;
using Serilog.Formatting.Json;
// using Serilog.Enrichers; // ActivityEnricher não suportado em net8.0


var builder = WebApplication.CreateBuilder(args);

try
{
    Console.WriteLine("[Program] Antes de UseSerilog");
    // Substitui o logger padrão pelo Serilog
    builder.Host.UseSerilog();
    Console.WriteLine("[Program] Depois de UseSerilog");

    // Configuração global do DogStatsD para métricas customizadas
    StatsdClient.Metrics.Configure(new StatsdClient.MetricsConfig
    {
        StatsdServerName = "datadog-agent.default.svc.cluster.local",
        StatsdServerPort = 8125
    });
    // Envia métrica de teste no startup global
    StatsdClient.Metrics.Counter("echo_teste.metric", 1);

    Log.Information("Iniciando a configuração da API Oficina Cardozo...");

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.Configure<ConfiguracoesJwt>(builder.Configuration.GetSection("ConfiguracoesJwt"));
    // ...demais configurações de serviços...

    // Swagger
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Oficina Cardozo API",
            Version = "v1",
            Description = "API para gerenciamento da Oficina Cardozo"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement()
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
                    In = ParameterLocation.Header,
                },
                new List<string>()
            }
        });
    });

    // ...demais configurações de banco, JWT, DI, etc...

    var app = builder.Build();

    // Middleware para correlação de requisições
    app.Use(async (context, next) =>
    {
        Serilog.Context.LogContext.PushProperty("CorrelationId", context.TraceIdentifier);
        await next();
    });

    Log.Information("📋 Configurando Swagger...");
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Oficina Cardozo API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Oficina Cardozo API - Swagger UI";
    });

    // Logging de requisições para diagnóstico (Lambda CloudWatch)
    var isLambda = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME"));
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

    Log.Information("🔐 Configurando CORS, Authentication e Authorization...");
    app.UseCors("AllowAll");

    // Middleware de latência do Datadog (deve vir após UseRouting e antes dos controllers)
    app.UseRouting();
    app.UseMiddleware<OficinaCardozo.API.Middleware.DatadogLatencyMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Log.Information("✅ Aplicação configurada e pronta para iniciar.");

    app.Run();
    Log.CloseAndFlush();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💥 ERRO FATAL: A aplicação falhou ao iniciar.");
    Log.CloseAndFlush();
    Environment.Exit(1);
}

// Torna a classe Program acessível para AWS Lambda
// Necessário para ASP.NET Core 6+ com minimal APIs
public partial class Program { }
