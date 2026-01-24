using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OficinaCardozo.Application.Interfaces;
using OficinaCardozo.Application.Mappers;
using OficinaCardozo.Application.Services;
using OficinaCardozo.Domain.Entities;
using OficinaCardozo.Domain.Interfaces;
using OficinaCardozo.Domain.Interfaces.Repositories;
using OficinaCardozo.Domain.Interfaces.Services;
using OficinaCardozo.Infrastructure.Data;
using OficinaCardozo.Infrastructure.Repositories;
using Xunit;

namespace OficinaCardozo.Tests.TestBase;

public abstract class IntegrationTestBase : IAsyncDisposable
{
    protected readonly IServiceScope _scope;
    protected readonly OficinaDbContext _context;

    protected readonly IOrdemServicoService OrdemServicoService;
    protected readonly IClienteService ClienteService;
    protected readonly IServicoService ServicoService;
    protected readonly IVeiculoService VeiculoService;

    protected IntegrationTestBase()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);

        var serviceProvider = services.BuildServiceProvider();
        _scope = serviceProvider.CreateScope();

        _context = _scope.ServiceProvider.GetRequiredService<OficinaDbContext>();
        OrdemServicoService = _scope.ServiceProvider.GetRequiredService<IOrdemServicoService>();
        ClienteService = _scope.ServiceProvider.GetRequiredService<IClienteService>();
        ServicoService = _scope.ServiceProvider.GetRequiredService<IServicoService>();
        VeiculoService = _scope.ServiceProvider.GetRequiredService<IVeiculoService>();

        InitializeDatabaseAsync().GetAwaiter().GetResult();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<OficinaDbContext>(options =>
        {
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");

            // Configurações específicas para testes
            options.ConfigureWarnings(warnings =>
            {
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning);
            });

#if DEBUG
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
#endif
        });

        // Repositories
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IVeiculoRepository, VeiculoRepository>();
        services.AddScoped<IServicoRepository, ServicoRepository>();
        services.AddScoped<IPecaRepository, PecaRepository>();
        services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();
        services.AddScoped<IOrcamentoRepository, OrcamentoRepository>();
        services.AddScoped<IOrdemServicoStatusRepository, OrdemServicoStatusRepository>();
        services.AddScoped<IOrcamentoStatusRepository, OrcamentoStatusRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        // Mappers
        services.AddScoped<IClienteMapper, ClienteMapper>();
        services.AddScoped<IVeiculoMapper, VeiculoMapper>();
        services.AddScoped<IServicoMapper, ServicoMapper>();

        // Application Services
        services.AddScoped<IAutenticacaoService, AutenticacaoService>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IVeiculoService, VeiculoService>();
        services.AddScoped<IServicoService, ServicoService>();
        services.AddScoped<IPecaService, PecaService>();
        services.AddScoped<IOrdemServicoService, OrdemServicoService>();
        services.AddScoped<ICpfCnpjValidationService, CpfCnpjValidationService>();
        services.AddScoped<IOrdemServicoStatusService, OrdemServicoStatusService>();
        services.AddScoped<IEmailMonitorService, EmailMonitorService>();
    }

    private async Task InitializeDatabaseAsync()
    {
        await _context.Database.EnsureCreatedAsync();

        // Garantir que os status essenciais existam sempre
        await SeedStatusDataAsync();
    }

    #region Métodos Auxiliares para Criação de Dados de Teste

    protected async Task<Cliente> CriarClienteTeste(string? cpf = null, string? nome = null)
    {
        var cliente = new Cliente
        {
            Nome = nome ?? $"Cliente Teste {DateTime.Now:HHmmss}",
            CpfCnpj = cpf ?? GerarCpfValido(),
            EmailPrincipal = $"cliente{DateTime.Now:HHmmss}@teste.com",
            TelefonePrincipal = "(11) 99999-9999",
            EnderecoPrincipal = "Rua de Teste, 123"
        };

        await _context.Clientes.AddAsync(cliente);
        await _context.SaveChangesAsync();
        return cliente;
    }

    protected async Task<Servico> CriarServicoTeste(string? nome = null, decimal preco = 150.00m)
    {
        var servico = new Servico
        {
            NomeServico = nome ?? $"Serviço Teste {DateTime.Now:HHmmss}",
            Preco = preco,
            TempoEstimadoExecucao = 2,
            DescricaoDetalhadaServico = "Serviço de teste para integração",
            FrequenciaRecomendada = "A cada 10.000 km"
        };

        await _context.Servicos.AddAsync(servico);
        await _context.SaveChangesAsync();
        return servico;
    }

    protected async Task<Veiculo> CriarVeiculoTeste(Cliente cliente, string? placa = null)
    {
        var veiculo = new Veiculo
        {
            IdCliente = cliente.Id,
            Placa = placa ?? GerarPlacaValida(),
            MarcaModelo = "Honda Civic Teste",
            AnoFabricacao = 2020,
            Cor = "Prata",
            TipoCombustivel = "Flex"
        };

        await _context.Veiculos.AddAsync(veiculo);
        await _context.SaveChangesAsync();
        return veiculo;
    }

    protected async Task<OrdemServico> CriarOrdemServicoTeste(Cliente cliente, Servico servico)
    {
        // Garante que os status existem no banco de dados em memória para os testes
        await SeedStatusDataAsync();

        var veiculo = await CriarVeiculoTeste(cliente);
        var statusRecebida = await _context.OrdensServicoStatus.FirstAsync(s => s.Descricao == "Recebida");

        var ordemServico = new OrdemServico
        {
            DataSolicitacao = DateTime.Now,
            IdVeiculo = veiculo.Id,
            IdStatus = statusRecebida.Id
        };

        await _context.OrdensServico.AddAsync(ordemServico);
        await _context.SaveChangesAsync();

        var ordemServicoServico = new OrdemServicoServico
        {
            IdOrdemServico = ordemServico.Id,
            IdServico = servico.Id,
            ValorAplicado = servico.Preco
        };

        await _context.OrdensServicoServicos.AddAsync(ordemServicoServico);
        await _context.SaveChangesAsync();

        return ordemServico;
    }

    protected async Task<Peca> CriarPecaTeste(string? nome = null, decimal preco = 50.00m, int estoque = 10)
    {
        var peca = new Peca
        {
            NomePeca = nome ?? $"Peça Teste {DateTime.Now:HHmmss}",
            CodigoIdentificador = $"PC{DateTime.Now:HHmmss}",
            Preco = preco,
            QuantidadeEstoque = estoque,
            QuantidadeMinima = 1,
            UnidadeMedida = "UN"
        };

        await _context.Pecas.AddAsync(peca);
        await _context.SaveChangesAsync();
        return peca;
    }

    /// <summary>
    /// Busca status por descrição de forma case-insensitive
    /// </summary>
    protected async Task<OrdemServicoStatus?> BuscarStatusOrdemServicoAsync(string descricao)
    {
        return await _context.OrdensServicoStatus
            .FirstOrDefaultAsync(s => s.Descricao.ToLower() == descricao.ToLower());
    }

    /// <summary>
    /// Busca status de orçamento por descrição de forma case-insensitive
    /// </summary>
    protected async Task<OrcamentoStatus?> BuscarStatusOrcamentoAsync(string descricao)
    {
        return await _context.OrcamentoStatus
            .FirstOrDefaultAsync(s => s.Descricao.ToLower() == descricao.ToLower());
    }

    private async Task SeedStatusDataAsync()
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (!await _context.OrdensServicoStatus.AnyAsync())
            {
                var statusOrdemServico = new[]
                {
                    new OrdemServicoStatus { Id = 1, Descricao = "Recebida" },
                    new OrdemServicoStatus { Id = 2, Descricao = "Em diagnostico" },
                    new OrdemServicoStatus { Id = 3, Descricao = "Aguardando aprovacao" },
                    new OrdemServicoStatus { Id = 4, Descricao = "Em execucao" },
                    new OrdemServicoStatus { Id = 5, Descricao = "Finalizada" },
                    new OrdemServicoStatus { Id = 6, Descricao = "Entregue" },
                    new OrdemServicoStatus { Id = 7, Descricao = "Em elaboracao" },
                    new OrdemServicoStatus { Id = 8, Descricao = "Cancelada" },
                    new OrdemServicoStatus { Id = 9, Descricao = "Devolvida" }
                };

                await _context.OrdensServicoStatus.AddRangeAsync(statusOrdemServico);
            }

            if (!await _context.OrcamentoStatus.AnyAsync())
            {
                var statusOrcamento = new[]
                {
                    new OrcamentoStatus { Id = 1, Descricao = "Criado" },
                    new OrcamentoStatus { Id = 2, Descricao = "Pendente aprovacao" },
                    new OrcamentoStatus { Id = 3, Descricao = "Aprovado" },
                    new OrcamentoStatus { Id = 4, Descricao = "Rejeitado" },
                    new OrcamentoStatus { Id = 5, Descricao = "Em elaboracao" }
                };

                await _context.OrcamentoStatus.AddRangeAsync(statusOrcamento);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    #endregion

    #region Métodos Utilitários

    protected static string GerarCpfValido()
    {
        var random = new Random();
        var cpf = new int[11];

        for (int i = 0; i < 9; i++)
            cpf[i] = random.Next(0, 10);

        int soma = 0;
        for (int i = 0; i < 9; i++)
            soma += cpf[i] * (10 - i);

        cpf[9] = (soma * 10) % 11;
        if (cpf[9] >= 10) cpf[9] = 0;

        soma = 0;
        for (int i = 0; i < 10; i++)
            soma += cpf[i] * (11 - i);

        cpf[10] = (soma * 10) % 11;
        if (cpf[10] >= 10) cpf[10] = 0;

        return string.Join("", cpf);
    }

    protected static string GerarPlacaValida()
    {
        var random = new Random();
        var letras = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var placa = "";

        for (int i = 0; i < 3; i++)
            placa += letras[random.Next(letras.Length)];

        for (int i = 0; i < 4; i++)
            placa += random.Next(0, 10);

        return placa;
    }

    protected async Task LimparDadosTesteAsync()
    {
        _context.OrdensServicoServicos.RemoveRange(_context.OrdensServicoServicos);
        _context.OrdensServicoPecas.RemoveRange(_context.OrdensServicoPecas);
        _context.Orcamentos.RemoveRange(_context.Orcamentos);
        _context.OrdensServico.RemoveRange(_context.OrdensServico);
        _context.Veiculos.RemoveRange(_context.Veiculos);
        _context.Clientes.RemoveRange(_context.Clientes);
        _context.Servicos.RemoveRange(_context.Servicos);
        _context.Pecas.RemoveRange(_context.Pecas);
        _context.Usuarios.RemoveRange(_context.Usuarios);

        await _context.SaveChangesAsync();

        await SeedStatusDataAsync();
    }

    /// <summary>
    /// Limpa TODOS os dados, incluindo status (usar com cuidado)
    /// </summary>
    protected async Task LimparTodosDadosIncluindoStatusAsync()
    {
        _context.OrdensServicoServicos.RemoveRange(_context.OrdensServicoServicos);
        _context.OrdensServicoPecas.RemoveRange(_context.OrdensServicoPecas);
        _context.Orcamentos.RemoveRange(_context.Orcamentos);
        _context.OrdensServico.RemoveRange(_context.OrdensServico);
        _context.Veiculos.RemoveRange(_context.Veiculos);
        _context.Clientes.RemoveRange(_context.Clientes);
        _context.Servicos.RemoveRange(_context.Servicos);
        _context.Pecas.RemoveRange(_context.Pecas);
        _context.Usuarios.RemoveRange(_context.Usuarios);
        _context.OrdensServicoStatus.RemoveRange(_context.OrdensServicoStatus);
        _context.OrcamentoStatus.RemoveRange(_context.OrcamentoStatus);

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Cleanup

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        _scope.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}