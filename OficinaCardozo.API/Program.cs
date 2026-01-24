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
using OficinaCardozo.Domain.Interfaces.Repositories;
using OficinaCardozo.Domain.Interfaces.Services;
using OficinaCardozo.Infrastructure.Data;
using OficinaCardozo.Infrastructure.Repositories;
using OficinaCardozo.Infrastructure.Services;
using System.Text;
using Serilog;
using Serilog.Formatting.Json;
// Serilog removido para teste de isolamento
// using Serilog.Enrichers; // ActivityEnricher não suportado em net8.0


var builder = WebApplication.CreateBuilder(args);
// Registro de todos os serviços de domínio e application necessários para os controllers
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IVeiculoService, VeiculoService>();
builder.Services.AddScoped<IServicoService, ServicoService>();
builder.Services.AddScoped<IPecaService, PecaService>();
builder.Services.AddScoped<IOrdemServicoService, OrdemServicoService>();
builder.Services.AddScoped<ICpfCnpjValidationService, CpfCnpjValidationService>();
builder.Services.AddScoped<IClienteMapper, ClienteMapper>();
builder.Services.AddScoped<IVeiculoMapper, VeiculoMapper>();
builder.Services.AddScoped<IServicoMapper, ServicoMapper>();
// Se existir extensão AddApplicationServices, garantir chamada
// builder.Services.AddApplicationServices();
// Configuração JWT Authentication (compatível com ConfigMap/Secret)
var jwtKey = builder.Configuration["ConfiguracoesJwt:ChaveSecreta"] ?? "sua-chave-jwt-super-secreta";
var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey));
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireCpf", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => c.Type == "cpf") ||
            context.User.HasClaim(c => c.Type == "cpfCnpj")
        )
    );
});

// Diagnóstico: logar variáveis de ambiente do banco
Console.WriteLine($"[DIAG] DB_CONNECTION: {Environment.GetEnvironmentVariable("DB_CONNECTION")}");
Console.WriteLine($"[DIAG] DB_HOST: {Environment.GetEnvironmentVariable("DB_HOST")}");
Console.WriteLine($"[DIAG] DB_USER: {Environment.GetEnvironmentVariable("DB_USER")}");
Console.WriteLine($"[DIAG] DB_DATABASE: {Environment.GetEnvironmentVariable("DB_DATABASE")}");
Console.WriteLine($"[DIAG] ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");

try
{
    // Serilog removido para teste de isolamento

    // Configuração global do DogStatsD para métricas customizadas
    StatsdClient.Metrics.Configure(new StatsdClient.MetricsConfig
    {
        StatsdServerName = "datadog-agent.default.svc.cluster.local",
        StatsdServerPort = 8125
    });
    // Envia métrica de teste no startup global
    StatsdClient.Metrics.Counter("echo_teste.metric", 1);

    // Log removido (Serilog)

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.Configure<ConfiguracoesJwt>(builder.Configuration.GetSection("ConfiguracoesJwt"));
    // ...demais configurações de serviços...
        // Registro do serviço de autenticação
        builder.Services.AddScoped<IAutenticacaoService, AutenticacaoService>();
        builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
        builder.Services.AddScoped<IServicoRepository, ServicoRepository>();
        builder.Services.AddScoped<IPecaRepository, PecaRepository>();
        builder.Services.AddScoped<IVeiculoRepository, VeiculoRepository>();
        builder.Services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();
    builder.Services.AddScoped<IOrcamentoRepository, OrcamentoRepository>();
    builder.Services.AddScoped<IOrdemServicoStatusRepository, OrdemServicoStatusRepository>();
    builder.Services.AddScoped<IOrcamentoStatusRepository, OrcamentoStatusRepository>();

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

    var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    builder.Services.AddDbContext<OficinaDbContext>(options =>
        options.UseNpgsql(connectionString));
    builder.Services.AddInfrastructureServices();

    // Configuração do Serilog para logs estruturados em JSON
    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "OficinaCardozo.API")
        .WriteTo.Console(new JsonFormatter())
        .CreateLogger();

    builder.Host.UseSerilog();

    // Middleware para correlação de requisições
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger);

    // ...demais configurações de banco, JWT, DI, etc...

    var app = builder.Build();

    // Middleware para adicionar CorrelationId nos logs
    app.Use(async (context, next) =>
    {
        var correlationId = context.TraceIdentifier;
        Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId);
        await next();
    });

    // Log removido (Serilog)
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

    // Log removido (Serilog)
    app.UseCors("AllowAll");

    // Middleware de latência do Datadog (deve vir após UseRouting e antes dos controllers)
    app.UseRouting();
    app.UseMiddleware<OficinaCardozo.API.Middleware.DatadogLatencyMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Log removido (Serilog)

    app.Run();
    // Log removido (Serilog)
}
catch (Exception ex)
{
    Console.WriteLine($"[Program] ERRO FATAL: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
    Environment.Exit(1);
}

// Torna a classe Program acessível para AWS Lambda
// Necessário para ASP.NET Core 6+ com minimal APIs
public partial class Program { }
