using OficinaCardozo.Application.Interfaces;
using OficinaCardozo.Application.Mappers;
using OficinaCardozo.Application.Services;
using OficinaCardozo.Domain.Interfaces;
using OficinaCardozo.Domain.Interfaces.Services;

namespace OficinaCardozo.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IVeiculoService, VeiculoService>();
        services.AddScoped<IServicoService, ServicoService>();
        services.AddScoped<IPecaService, PecaService>();
        services.AddScoped<IOrdemServicoService, OrdemServicoService>();

        // Mappers
        services.AddScoped<IClienteMapper, ClienteMapper>();
        services.AddScoped<IVeiculoMapper, VeiculoMapper>();
        services.AddScoped<IServicoMapper, ServicoMapper>();

        // Domain Services
        services.AddScoped<ICpfCnpjValidationService, CpfCnpjValidationService>();

        return services;
    }
}