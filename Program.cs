using OFICINACARDOZO.BILLINGSERVICE;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
    options.Filters.Add<OFICINACARDOZO.BILLINGSERVICE.API.ValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "OficinaCardozo Billing Service API",
        Version = "v1",
        Description = "API para gestão de Ordens de Serviço.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Equipe OficinaCardozo",
            Email = "contato@oficinacardozo.com"
        }
    });
    // options.EnableAnnotations(); // Removido: método não existe
});
builder.Services.AddHealthChecks();

// Configuração do DbContext para PostgreSQL via variáveis de ambiente
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "billingservice";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";
var postgresConnectionString = $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPassword};sslmode=Require";
builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseNpgsql(postgresConnectionString));

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware global de tratamento de exceções
app.UseMiddleware<OFICINACARDOZO.BILLINGSERVICE.API.ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
