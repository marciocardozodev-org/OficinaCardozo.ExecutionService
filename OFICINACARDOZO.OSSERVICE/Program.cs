using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using OFICINACARDOZO.OSSERVICE.InfraDb;
using OFICINACARDOZO.OSSERVICE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<OFICINACARDOZO.OSSERVICE.API.ValidationFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "OficinaCardozo OS Service API",
        Version = "v1",
        Description = "API para gestão de Ordens de Serviço.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Equipe OficinaCardozo",
            Email = "contato@oficinacardozo.com"
        }
    });
    options.EnableAnnotations();
});
builder.Services.AddHealthChecks();

// Configuração do DbContext InMemory (ajuste para outro provedor se necessário)
builder.Services.AddDbContext<OsDbContext>(options =>
    options.UseInMemoryDatabase("OsDb"));

// Repositório EF
builder.Services.AddScoped<OrdemDeServicoEfRepository>();


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware global de tratamento de exceções
app.UseMiddleware<OFICINACARDOZO.OSSERVICE.API.ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
