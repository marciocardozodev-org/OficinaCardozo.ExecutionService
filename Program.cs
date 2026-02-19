// Trigger pipeline - alteração forçada
// Alteração técnica para acionar pipeline (trigger)
using OFICINACARDOZO.EXECUTIONSERVICE;
using Amazon.SQS;
using Amazon.SimpleNotificationService;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OficinaCardozo.ExecutionService.Domain;
using OficinaCardozo.ExecutionService.Inbox;
using OficinaCardozo.ExecutionService.Outbox;
using OficinaCardozo.ExecutionService.EventHandlers;
using OficinaCardozo.ExecutionService.Messaging;
using OficinaCardozo.ExecutionService.Workers;

var builder = WebApplication.CreateBuilder(args);

// Configuração do JWT (chave de exemplo, troque para produção)
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "chave-super-secreta-para-dev";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
    };
});

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<OFICINACARDOZO.EXECUTIONSERVICE.API.ValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "OficinaCardozo Execution Service API",
        Version = "v1",
        Description = "API para gestão de Ordens de Serviço com processamento assíncrono e confiável.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Equipe OficinaCardozo",
            Email = "contato@oficinacardozo.com"
        }
    });
});

builder.Services.AddScoped<OFICINACARDOZO.EXECUTIONSERVICE.Application.ExecucaoOsService>();
builder.Services.AddScoped<OFICINACARDOZO.EXECUTIONSERVICE.Application.AtualizacaoStatusOsService>();
builder.Services.AddScoped<OFICINACARDOZO.EXECUTIONSERVICE.Application.ServiceOrchestrator>();
builder.Services.AddHealthChecks();

// Configuração do DbContext para PostgreSQL via variáveis de ambiente
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "executionservice";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";
var postgresConnectionString = $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPassword};sslmode=Require";
builder.Services.AddDbContext<ExecutionDbContext>(options =>
    options.UseNpgsql(postgresConnectionString));

// ============= EXECUTIONSERVICE MESSAGING & EXECUTION FLOW =============

// Configuração de Messaging (SQS/SNS)
// Usa variáveis do ConfigMap aws-messaging-config (produção) com fallback para LocalStack (dev)
var inputQueue = Environment.GetEnvironmentVariable("AWS_SQS_QUEUE_BILLING") 
    ?? Environment.GetEnvironmentVariable("SQS_QUEUE_URL") 
    ?? "http://localhost:9324/queue/billing-events";

var outputTopic = Environment.GetEnvironmentVariable("AWS_SNS_TOPIC_EXECUTION_EVENTS") 
    ?? Environment.GetEnvironmentVariable("SNS_TOPIC_ARN") 
    ?? "arn:aws:sns:us-east-1:000000000000:execution-events";

var messagingConfig = new MessagingConfig
{
    InputQueue = inputQueue,
    OutputTopic = outputTopic
};
builder.Services.AddSingleton(messagingConfig);

// AWS Services
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();

// Serviços em memória (substituir por DB futuramente)
builder.Services.AddSingleton<List<ExecutionJob>>();

// Serviços de Inbox/Outbox
builder.Services.AddSingleton<IInboxService, InboxService>();
builder.Services.AddSingleton<IOutboxService, OutboxService>();

// Event Handlers
builder.Services.AddScoped<PaymentConfirmedHandler>();
builder.Services.AddScoped<OsCanceledHandler>();

// Background Services (Workers e Consumidores)
builder.Services.AddHostedService<ExecutionWorker>();
builder.Services.AddHostedService<SqsConsumer>();
builder.Services.AddHostedService<SnsPublisher>();

// ========================================================================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware global de tratamento de exceções
app.UseMiddleware<OFICINACARDOZO.EXECUTIONSERVICE.API.ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
