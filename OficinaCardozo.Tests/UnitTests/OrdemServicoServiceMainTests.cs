using Xunit;
using Moq;
using OficinaCardozo.Application.Services;
using OficinaCardozo.Application.DTOs;
using OficinaCardozo.Domain.Entities;
using OficinaCardozo.Domain.Interfaces;
using OficinaCardozo.Domain.Interfaces.Repositories;
using FluentAssertions;

namespace OficinaCardozo.Tests.UnitTests.Services;

public class OrdemServicoServiceMainTests
{
    private readonly Mock<IOrdemServicoRepository> _mockOrdemServicoRepository;
    private readonly Mock<IClienteRepository> _mockClienteRepository;
    private readonly Mock<IVeiculoRepository> _mockVeiculoRepository;
    private readonly Mock<IServicoRepository> _mockServicoRepository;
    private readonly Mock<IOrdemServicoStatusRepository> _mockStatusRepository;
    private readonly Mock<IOrcamentoRepository> _mockOrcamentoRepository;
    private readonly Mock<IOrcamentoStatusRepository> _mockOrcamentoStatusRepository;
    private readonly OrdemServicoService _service;

    private const string STATUS_RECEBIDA = "Recebida";
    private const string STATUS_EM_DIAGNOSTICO = "Em diagnostico";      
    private const string STATUS_AGUARDANDO_APROVACAO = "Aguardando aprovacao";  
    private const string STATUS_EM_EXECUCAO = "Em execucao";             
    private const string STATUS_EM_ELABORACAO = "Em elaboracao";         
    private const string STATUS_FINALIZADA = "Finalizada";

    private const string STATUS_ORC_CRIADO = "Criado";
    private const string STATUS_ORC_PENDENTE = "Pendente aprovacao";    
    private const string STATUS_ORC_APROVADO = "Aprovado";
    private const string STATUS_ORC_REJEITADO = "Rejeitado";


    public OrdemServicoServiceMainTests()
    {
        _mockOrdemServicoRepository = new Mock<IOrdemServicoRepository>();
        _mockClienteRepository = new Mock<IClienteRepository>();
        _mockVeiculoRepository = new Mock<IVeiculoRepository>();
        _mockServicoRepository = new Mock<IServicoRepository>();
        _mockStatusRepository = new Mock<IOrdemServicoStatusRepository>();
        _mockOrcamentoRepository = new Mock<IOrcamentoRepository>();
        _mockOrcamentoStatusRepository = new Mock<IOrcamentoStatusRepository>();

        _service = new OrdemServicoService(
            _mockOrdemServicoRepository.Object,
            _mockClienteRepository.Object,
            _mockVeiculoRepository.Object,
            _mockServicoRepository.Object,
            Mock.Of<IPecaRepository>(),
            _mockOrcamentoRepository.Object,
            _mockStatusRepository.Object,
            _mockOrcamentoStatusRepository.Object
        );
    }

    #region Cria��o de Ordem de Servi�o

    [Fact]
    public async Task CriarOrdemServico_ComDadosValidos_DeveCriarComSucesso()
    {
        // Arrange
        var createDto = new CreateOrdemServicoDto
        {
            ClienteCpfCnpj = "12345678901",
            VeiculoPlaca = "ABC1234",
            VeiculoMarcaModelo = "Honda Civic",
            VeiculoAnoFabricacao = 2020,
            ServicosIds = new List<int> { 1 }
        };

        var cliente = new Cliente { Id = 1, CpfCnpj = "12345678901", Nome = "Jo�o" };
        var veiculo = new Veiculo { Id = 1, IdCliente = 1, Placa = "ABC1234" };
        var servico = new Servico { Id = 1, NomeServico = "Troca de �leo", Preco = 100m };
        var status = new OrdemServicoStatus { Id = 1, Descricao = STATUS_RECEBIDA };
        var ordemCriada = new OrdemServico { Id = 1, IdVeiculo = 1, IdStatus = 1 };

        _mockClienteRepository.Setup(x => x.GetByCpfCnpjAsync("12345678901")).ReturnsAsync(cliente);
        _mockVeiculoRepository.Setup(x => x.GetByPlacaAsync("ABC1234")).ReturnsAsync(veiculo);
        _mockServicoRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(servico);
        _mockStatusRepository.Setup(x => x.GetByDescricaoAsync(STATUS_RECEBIDA)).ReturnsAsync(status);
        _mockOrdemServicoRepository.Setup(x => x.CreateAsync(It.IsAny<OrdemServico>())).ReturnsAsync(ordemCriada);
        _mockOrdemServicoRepository.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(ordemCriada);

        // Act
        var resultado = await _service.CreateOrdemServicoComOrcamentoAsync(createDto);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Id.Should().Be(1);
        _mockOrdemServicoRepository.Verify(x => x.CreateAsync(It.IsAny<OrdemServico>()), Times.Once);
    }

    [Fact]
    public async Task CriarOrdemServico_ClienteNaoExiste_DeveLancarExcecao()
    {
        // Arrange
        var createDto = new CreateOrdemServicoDto { ClienteCpfCnpj = "99999999999" };
        _mockClienteRepository.Setup(x => x.GetByCpfCnpjAsync("99999999999")).ReturnsAsync((Cliente?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CreateOrdemServicoComOrcamentoAsync(createDto));
    }

    #endregion

    #region Fluxo de Diagn�stico

    [Fact]
    public async Task IniciarDiagnostico_ComStatusCorreto_DeveIniciarComSucesso()
    {
        // Arrange
        var ordem = new OrdemServico
        {
            Id = 1,
            IdStatus = 1,
            Status = new OrdemServicoStatus { Id = 1, Descricao = STATUS_RECEBIDA }
        };
        var statusRecebida = new OrdemServicoStatus { Id = 1, Descricao = STATUS_RECEBIDA };
        var statusDiagnostico = new OrdemServicoStatus { Id = 2, Descricao = STATUS_EM_DIAGNOSTICO };
        var statusOrcamento = new OrcamentoStatus { Id = 1, Descricao = STATUS_ORC_CRIADO };
        var orcamento = new Orcamento { Id = 1, IdOrdemServico = 1 };

        _mockOrdemServicoRepository.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(ordem);
        _mockStatusRepository.Setup(x => x.GetByDescricaoAsync(STATUS_RECEBIDA)).ReturnsAsync(statusRecebida);
        _mockStatusRepository.Setup(x => x.GetByDescricaoAsync(STATUS_EM_DIAGNOSTICO)).ReturnsAsync(statusDiagnostico);
        _mockOrdemServicoRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(ordem);
        _mockOrcamentoStatusRepository.Setup(x => x.GetByDescricaoAsync(STATUS_ORC_CRIADO)).ReturnsAsync(statusOrcamento);
        _mockOrcamentoRepository.Setup(x => x.CreateAsync(It.IsAny<Orcamento>())).ReturnsAsync(orcamento);

        // Act
        var resultado = await _service.IniciarDiagnosticoAsync(1);

        // Assert
        resultado.Should().NotBeNull();
        resultado.StatusDescricao.Should().Be("Diagnostico iniciado");
        _mockOrcamentoRepository.Verify(x => x.CreateAsync(It.IsAny<Orcamento>()), Times.Once);
    }

    #endregion

    #region Aprova��o de Or�amento

    [Fact]
    public async Task AprovarOrcamento_ComAprovacao_DeveIniciarExecucao()
    {
        // Arrange
        var aprovarDto = new AprovarOrcamentoDto { IdOrcamento = 1, Aprovado = true };
        var orcamento = new Orcamento
        {
            Id = 1,
            IdOrdemServico = 1,
            Status = new OrcamentoStatus { Descricao = STATUS_ORC_PENDENTE }, // USANDO CONSTANTE
            OrdemServico = new OrdemServico
            {
                Id = 1,
                IdStatus = 3,
                Status = new OrdemServicoStatus { Descricao = STATUS_AGUARDANDO_APROVACAO }
            }
        };

        var statusAguardando = new OrdemServicoStatus { Id = 3, Descricao = STATUS_AGUARDANDO_APROVACAO };
        var statusExecucao = new OrdemServicoStatus { Id = 4, Descricao = STATUS_EM_EXECUCAO };
        var statusAprovado = new OrcamentoStatus { Id = 3, Descricao = STATUS_ORC_APROVADO };

        _mockOrcamentoRepository.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(orcamento);
        _mockStatusRepository.Setup(x => x.GetByDescricaoAsync(STATUS_AGUARDANDO_APROVACAO)).ReturnsAsync(statusAguardando);
        _mockStatusRepository.Setup(x => x.GetByDescricaoAsync(STATUS_EM_EXECUCAO)).ReturnsAsync(statusExecucao);
        _mockOrcamentoStatusRepository.Setup(x => x.GetByDescricaoAsync(STATUS_ORC_APROVADO)).ReturnsAsync(statusAprovado);
        _mockOrdemServicoRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(orcamento.OrdemServico);

        // Act
        var resultado = await _service.AprovarOrcamentoAsync(aprovarDto);

        // Assert
        resultado.Should().NotBeNull();
        resultado.StatusDescricao.Should().Be(STATUS_ORC_APROVADO);
        resultado.MensagemAprovacao.Should().Contain("aprovado");
        _mockOrdemServicoRepository.Verify(x => x.UpdateAsync(It.IsAny<OrdemServico>()), Times.Once);
    }

    [Fact]
    public async Task AprovarOrcamento_ComRejeicao_DeveCriarNovoOrcamento()
    {
        // Arrange
        var aprovarDto = new AprovarOrcamentoDto
        {
            IdOrcamento = 1,
            Aprovado = false,
            SolicitarNovoOrcamento = true
        };

        var orcamento = new Orcamento
        {
            Id = 1,
            IdOrdemServico = 1,
            Status = new OrcamentoStatus { Descricao = STATUS_ORC_PENDENTE }, // USANDO CONSTANTE
            OrdemServico = new OrdemServico { IdStatus = 3 }
        };

        var novoOrcamento = new Orcamento { Id = 2, IdOrdemServico = 1 };

        _mockOrcamentoRepository.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(orcamento);
        _mockStatusRepository.Setup(x => x.GetByDescricaoAsync(STATUS_AGUARDANDO_APROVACAO))
            .ReturnsAsync(new OrdemServicoStatus { Id = 3, Descricao = STATUS_AGUARDANDO_APROVACAO });
        _mockStatusRepository.Setup(x => x.GetByDescricaoAsync(STATUS_EM_ELABORACAO))
            .ReturnsAsync(new OrdemServicoStatus { Id = 7, Descricao = STATUS_EM_ELABORACAO });
        _mockOrcamentoStatusRepository.Setup(x => x.GetByDescricaoAsync(STATUS_ORC_REJEITADO))
            .ReturnsAsync(new OrcamentoStatus { Descricao = STATUS_ORC_REJEITADO });
        _mockOrcamentoStatusRepository.Setup(x => x.GetByDescricaoAsync(STATUS_ORC_CRIADO))
            .ReturnsAsync(new OrcamentoStatus { Descricao = STATUS_ORC_CRIADO });
        _mockOrcamentoRepository.Setup(x => x.CreateAsync(It.IsAny<Orcamento>())).ReturnsAsync(novoOrcamento);

        // Act
        var resultado = await _service.AprovarOrcamentoAsync(aprovarDto);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Id.Should().Be(2); // Novo or�amento criado
        _mockOrcamentoRepository.Verify(x => x.CreateAsync(It.IsAny<Orcamento>()), Times.Once);
    }

    #endregion

    #region Finaliza��o e Entrega

    [Fact]
    public async Task FinalizarServico_ComStatusCorreto_DeveRegistrarDataFinalizacao()
    {
        // Arrange
        var ordem = new OrdemServico
        {
            Id = 1,
            IdStatus = 4,
            Status = new OrdemServicoStatus { Descricao = STATUS_EM_EXECUCAO }
        };

        var statusExecucao = new OrdemServicoStatus { Id = 4, Descricao = STATUS_EM_EXECUCAO };
        var statusFinalizada = new OrdemServicoStatus { Id = 5, Descricao = STATUS_FINALIZADA };
        var ordemFinalizada = new OrdemServico
        {
            Id = 1,
            DataFinalizacao = DateTime.Now,
            Status = new OrdemServicoStatus { Descricao = STATUS_FINALIZADA }
        };

        _mockOrdemServicoRepository.Setup(x => x.GetByIdWithDetailsAsync(1)).ReturnsAsync(ordem);
        _mockStatusRepository.Setup(x => x.GetByDescricaoAsync(STATUS_EM_EXECUCAO)).ReturnsAsync(statusExecucao);
        _mockStatusRepository.Setup(x => x.GetByDescricaoAsync(STATUS_FINALIZADA)).ReturnsAsync(statusFinalizada);
        _mockOrdemServicoRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(ordem);
        _mockOrdemServicoRepository.SetupSequence(x => x.GetByIdWithDetailsAsync(1))
            .ReturnsAsync(ordem)
            .ReturnsAsync(ordemFinalizada);

        // Act
        var resultado = await _service.FinalizarServicoAsync(1);

        // Assert
        resultado.Should().NotBeNull();
        resultado.StatusDescricao.Should().Be(STATUS_FINALIZADA);
        resultado.DataFinalizacao.Should().NotBeNull();
        _mockOrdemServicoRepository.Verify(x => x.UpdateAsync(It.IsAny<OrdemServico>()), Times.Once);
    }

    #endregion
}