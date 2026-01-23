using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OficinaCardozo.Application.DTOs;
using OficinaCardozo.Application.Interfaces;
using OficinaCardozo.Application.Services;

namespace OficinaCardozo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdensServicoController : ControllerBase
{
    private readonly ILogger<OrdensServicoController> _logger;
    private readonly IOrdemServicoService _ordemServicoService;
    private readonly IServicoService _servicoService;
    private readonly IPecaService _pecaService;

    public OrdensServicoController(
        IOrdemServicoService ordemServicoService,
        IServicoService servicoService,
        IPecaService pecaService,
        ILogger<OrdensServicoController> logger)
    {
        _ordemServicoService = ordemServicoService;
        _servicoService = servicoService;
        _pecaService = pecaService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrdemServicoDto>>> GetAll()
    {
        _logger.LogInformation("GetAll endpoint chamado");
        try
        {
            var ordensServico = await _ordemServicoService.GetAllAtivasAsync();
            _logger.LogInformation("GetAll retornou {Count} ordens", ordensServico?.Count() ?? 0);
            return Ok(ordensServico);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ordens de serviço");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpGet("todas")]
    public async Task<ActionResult<IEnumerable<OrdemServicoDto>>> GetAllIncludingFinalized()
    {
        _logger.LogInformation("GetAllIncludingFinalized endpoint chamado");
        try
        {
            var ordensServico = await _ordemServicoService.GetAllAsync();
            _logger.LogInformation("GetAllIncludingFinalized retornou {Count} ordens", ordensServico?.Count() ?? 0);
            return Ok(ordensServico);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ordens de serviço incluindo finalizadas");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpGet("ativas")]
    public async Task<ActionResult<IEnumerable<OrdemServicoDto>>> GetAllAtivas()
    {
        _logger.LogInformation("GetAllAtivas endpoint chamado");
        try
        {
            var ordensServico = await _ordemServicoService.GetAllAtivasAsync();
            _logger.LogInformation("GetAllAtivas retornou {Count} ordens", ordensServico?.Count() ?? 0);
            return Ok(ordensServico);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ordens de serviço ativas");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpGet("servicos-selecao")]
    public async Task<ActionResult<IEnumerable<ServicoSelecaoDto>>> GetServicosParaSelecao()
    {
        _logger.LogInformation("GetServicosParaSelecao endpoint chamado");
        try
        {
            var servicos = await _servicoService.GetAllAsync();
            var servicosSelecao = servicos.Select(s => new ServicoSelecaoDto
            {
                Id = s.Id,
                NomeServico = s.NomeServico,
                Preco = s.Preco,
                TempoEstimadoExecucao = s.TempoEstimadoExecucao,
                DescricaoDetalhadaServico = s.DescricaoDetalhadaServico
            });

            _logger.LogInformation("GetServicosParaSelecao retornou {Count} serviços", servicosSelecao.Count());
            return Ok(servicosSelecao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar serviços para seleção");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpGet("pecas-selecao")]
    public async Task<ActionResult<IEnumerable<PecaSelecaoDto>>> GetPecasParaSelecao()
    {
        _logger.LogInformation("GetPecasParaSelecao endpoint chamado");
        try
        {
            var pecas = await _pecaService.GetAllAsync();
            var pecasSelecao = pecas.Select(p => new PecaSelecaoDto
            {
                Id = p.Id,
                NomePeca = p.NomePeca,
                CodigoIdentificador = p.CodigoIdentificador,
                Preco = p.Preco,
                QuantidadeEstoque = p.QuantidadeEstoque,
                UnidadeMedida = p.UnidadeMedida,
                EstoqueBaixo = p.EstoqueBaixo
            });

            _logger.LogInformation("GetPecasParaSelecao retornou {Count} peças", pecasSelecao.Count());
            return Ok(pecasSelecao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar peças para seleção");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpGet("pecas-com-estoque")]
    public async Task<ActionResult<IEnumerable<PecaSelecaoDto>>> GetPecasComEstoque()
    {
        _logger.LogInformation("GetPecasComEstoque endpoint chamado");
        try
        {
            var pecas = await _pecaService.GetAllAsync();
            var pecasComEstoque = pecas
                .Where(p => p.QuantidadeEstoque > 0)
                .Select(p => new PecaSelecaoDto
                {
                    Id = p.Id,
                    NomePeca = p.NomePeca,
                    CodigoIdentificador = p.CodigoIdentificador,
                    Preco = p.Preco,
                    QuantidadeEstoque = p.QuantidadeEstoque,
                    UnidadeMedida = p.UnidadeMedida,
                    EstoqueBaixo = p.EstoqueBaixo
                });

            _logger.LogInformation("GetPecasComEstoque retornou {Count} peças com estoque", pecasComEstoque.Count());
            return Ok(pecasComEstoque);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar peças com estoque");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<OrdemServicoDto>> CreateOrdemServicoComOrcamento([FromBody] CreateOrdemServicoDto createDto)
    {
        _logger.LogInformation("CreateOrdemServicoComOrcamento endpoint chamado");
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ordemServico = await _ordemServicoService.CreateOrdemServicoComOrcamentoAsync(createDto);
            _logger.LogInformation("Ordem de serviço criada com sucesso: {Id}", ordemServico.Id);
            return CreatedAtAction(nameof(GetById), new { id = ordemServico.Id }, ordemServico);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Recurso não encontrado ao criar ordem de serviço");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao criar ordem de serviço");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar ordem de serviço");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrdemServicoDto>> GetById(int id)
    {
        _logger.LogInformation("GetById endpoint chamado para ID {Id}", id);
        try
        {
            var ordemServico = await _ordemServicoService.GetByIdAsync(id);
            if (ordemServico == null)
            {
                _logger.LogWarning("Ordem de serviço não encontrada: {Id}", id);
                return NotFound(new { message = "Ordem de servi�o n�o encontrada" });
            }

            _logger.LogInformation("Ordem de serviço encontrada: {Id}", id);
            return Ok(ordemServico);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar ordem de serviço por ID");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpGet("orcamentos")]
    public async Task<ActionResult<IEnumerable<OrcamentoDto>>> GetAllOrcamentos()
    {
        _logger.LogInformation("GetAllOrcamentos endpoint chamado");
        try
        {
            var orcamentos = await _ordemServicoService.GetAllOrcamentosAsync();
            _logger.LogInformation("GetAllOrcamentos retornou {Count} orçamentos", orcamentos?.Count() ?? 0);
            return Ok(orcamentos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar orçamentos");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost("{id}/iniciar-diagnostico")]
    [Authorize(Policy = "RequireCpf")]
    public async Task<ActionResult<OrcamentoResumoDto>> IniciarDiagnostico(int id)
    {
        _logger.LogInformation("IniciarDiagnostico endpoint chamado para ID {Id}", id);
        try
        {
            var resultado = await _ordemServicoService.IniciarDiagnosticoAsync(id);
            _logger.LogInformation("Diagnóstico iniciado com sucesso para a ordem de serviço ID {Id}", id);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Ordem de serviço não encontrada ao iniciar diagnóstico");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao iniciar diagnóstico");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar diagnóstico");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost("enviar-orcamento-para-aprovacao")]
    [Authorize(Policy = "RequireCpf")]
    public async Task<ActionResult<OrcamentoResumoDto>> EnviarOrcamentoParaAprovacao([FromBody] EnviarOrcamentoParaAprovacaoDto enviarDto)
    {
        _logger.LogInformation("EnviarOrcamentoParaAprovacao endpoint chamado");
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _ordemServicoService.EnviarOrcamentoParaAprovacaoAsync(enviarDto);
            _logger.LogInformation("Orçamento enviado para aprovação com sucesso: {Id}", resultado.Id);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Recurso não encontrado ao enviar orçamento para aprovação");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao enviar orçamento para aprovação");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar orçamento para aprovação");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost("{id}/finalizar-diagnostico")]
    [Authorize(Policy = "RequireCpf")]
    public async Task<ActionResult<OrcamentoResumoDto>> FinalizarDiagnostico(int id)
    {
        _logger.LogInformation("FinalizarDiagnostico endpoint chamado para ID {Id}", id);
        try
        {
            var resultado = await _ordemServicoService.FinalizarDiagnosticoAsync(id);
            _logger.LogInformation("Diagnóstico finalizado com sucesso para a ordem de serviço ID {Id}", id);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Ordem de serviço não encontrada ao finalizar diagnóstico");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao finalizar diagnóstico");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao finalizar diagnóstico");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost("aprovar-orcamento")]
    [Authorize(Policy = "RequireCpf")]
    public async Task<ActionResult<OrcamentoResumoDto>> AprovarOrcamento([FromBody] AprovarOrcamentoDto aprovarDto)
    {
        _logger.LogInformation("AprovarOrcamento endpoint chamado");
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _ordemServicoService.AprovarOrcamentoAsync(aprovarDto);
            _logger.LogInformation("Orçamento aprovado com sucesso: {Id}", resultado.Id);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Recurso não encontrado ao aprovar orçamento");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aprovar orçamento");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost("{id}/iniciar-execucao")]
    public async Task<ActionResult<OrdemServicoDto>> IniciarExecucao(int id)
    {
        _logger.LogInformation("IniciarExecucao endpoint chamado para ID {Id}", id);
        try
        {
            var resultado = await _ordemServicoService.IniciarExecucaoAsync(id);
            _logger.LogInformation("Execução iniciada com sucesso para a ordem de serviço ID {Id}", id);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Ordem de serviço não encontrada ao iniciar execução");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao iniciar execução");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar execução");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost("{id}/finalizar-servico")]
    [Authorize(Policy = "RequireCpf")]
    public async Task<ActionResult<OrdemServicoDto>> FinalizarServico(int id)
    {
        _logger.LogInformation("FinalizarServico endpoint chamado para ID {Id}", id);
        try
        {
            var resultado = await _ordemServicoService.FinalizarServicoAsync(id);
            _logger.LogInformation("Serviço finalizado com sucesso para a ordem de serviço ID {Id}", id);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Ordem de serviço não encontrada ao finalizar serviço");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao finalizar serviço");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao finalizar serviço");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost("{id}/entregar-veiculo")]
    [Authorize(Policy = "RequireCpf")]
    public async Task<ActionResult<OrdemServicoDto>> EntregarVeiculo(int id)
    {
        _logger.LogInformation("EntregarVeiculo endpoint chamado para ID {Id}", id);
        try
        {
            var resultado = await _ordemServicoService.EntregarVeiculoAsync(id);
            _logger.LogInformation("Veículo entregue com sucesso para a ordem de serviço ID {Id}", id);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Ordem de serviço não encontrada ao entregar veículo");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao entregar veículo");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao entregar veículo");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost("cancelar-ordem-servico")]
    public async Task<ActionResult<OrdemServicoDto>> CancelarOrdemServico([FromBody] CancelarOrdemServicoDto cancelarDto)
    {
        _logger.LogInformation("CancelarOrdemServico endpoint chamado");
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _ordemServicoService.CancelarOrdemServicoAsync(cancelarDto);
            _logger.LogInformation("Ordem de serviço cancelada com sucesso: {Id}", resultado.Id);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Recurso não encontrado ao cancelar ordem de serviço");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao cancelar ordem de serviço");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao cancelar ordem de serviço");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost("{id}/devolver-veiculo-sem-servico")]
    public async Task<ActionResult<OrdemServicoDto>> DevolverVeiculoSemServico(int id, [FromBody] string motivo)
    {
        _logger.LogInformation("DevolverVeiculoSemServico endpoint chamado para ID {Id}", id);
        try
        {
            var resultado = await _ordemServicoService.DevolverVeiculoSemServicoAsync(id, motivo);
            _logger.LogInformation("Veículo devolvido sem serviço com sucesso para a ordem de serviço ID {Id}", id);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Ordem de serviço não encontrada ao devolver veículo sem serviço");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao devolver veículo sem serviço");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao devolver veículo sem serviço");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost("responder-orcamento")]
    public async Task<ActionResult<OrcamentoResumoDto>> ResponderOrcamento([FromBody] AprovarOrcamentoDto aprovarDto)
    {
        _logger.LogInformation("ResponderOrcamento endpoint chamado");
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var resultado = await _ordemServicoService.AprovarOrcamentoAsync(aprovarDto);
            _logger.LogInformation("Orçamento respondido com sucesso: {Id}", resultado.Id);
            return Ok(resultado);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Recurso não encontrado ao responder orçamento");
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao responder orçamento");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao responder orçamento");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost("tempo-medio-execucao")]
    public async Task<ActionResult<TempoMedioExecucaoDto>> ObterTempoMedioExecucao([FromBody] FiltroTempoMedioDto? filtro = null)
    {
        _logger.LogInformation("ObterTempoMedioExecucao endpoint chamado");
        try
        {
            var resultado = await _ordemServicoService.ObterTempoMedioExecucaoAsync(filtro);
            _logger.LogInformation("ObterTempoMedioExecucao retornou com sucesso");
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operação inválida ao obter tempo médio de execução");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter tempo médio de execução");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpGet("resumo-tempo-execucao")]
    public async Task<ActionResult<object>> ObterResumoTempoExecucao()
    {
        _logger.LogInformation("ObterResumoTempoExecucao endpoint chamado");
        try
        {
            var resultado = await _ordemServicoService.ObterResumoTempoExecucaoAsync();

            var response = new
            {
                resumoExecutivo = new
                {
                    totalOrdens = resultado.EstatisticasGerais.TotalOrdensAnalisadas,
                    ordensEntregues = resultado.EstatisticasGerais.TotalOrdensEntregues,
                    tempoMedioHoras = Math.Round(resultado.EstatisticasGerais.TempoMedioEntregaHoras, 1),
                    tempoMedioFormatado = resultado.EstatisticasGerais.TempoMedioFormatado,
                    performance = resultado.EstatisticasGerais.PerformanceGeral,
                    mensagem = resultado.EstatisticasGerais.ResumoExecutivo
                },

                ordensRecentes = resultado.OrdensServico
                    .Take(10)
                    .Select(o => new
                    {
                        id = o.IdOrdemServico,
                        cliente = o.ClienteNome,
                        veiculo = $"{o.VeiculoPlaca} - {o.VeiculoMarcaModelo}",
                        dataInicio = o.DataSolicitacao.ToString("dd/MM/yyyy HH:mm"),
                        dataEntrega = o.DataEntrega?.ToString("dd/MM/yyyy HH:mm") ?? "n�o entregue",
                        status = o.StatusDescricao,
                        tempoHoras = Math.Round(o.TempoEntregaHoras ?? o.TempoFinalizacaoHoras ?? 0, 1),
                        tempoFormatado = o.TempoFormatado,
                        performance = o.IndicadorPerformance,
                        valor = $"R$ {o.ValorTotal:N2}"
                    }),

                topClientes = resultado.TempoMedioPorCliente
                    .Take(10)
                    .Select(c => new
                    {
                        cliente = c.NomeCliente,
                        totalServicos = c.TotalOrdensEntregues,
                        tempoMedioHoras = Math.Round(c.TempoMedioHoras, 1),
                        tempoFormatado = c.TempoMedioFormatado,
                        valorMedio = $"R$ {c.ValorMedioOrdem:N2}",
                        performance = c.Performance,
                        classificacao = c.ClassificacaoCliente
                    }),

                indicadores = new
                {
                    servicosRapidos = resultado.OrdensServico.Count(o => (o.TempoEntregaHoras ?? 999) <= 24),
                    servicosLentos = resultado.OrdensServico.Count(o => (o.TempoEntregaHoras ?? 0) > 120),
                    servicosEmAndamento = resultado.EstatisticasGerais.TotalOrdensAnalisadas - resultado.EstatisticasGerais.TotalOrdensEntregues,
                    percentualEficiencia = resultado.EstatisticasGerais.TotalOrdensEntregues > 0
                        ? Math.Round((double)resultado.EstatisticasGerais.TotalOrdensEntregues / resultado.EstatisticasGerais.TotalOrdensAnalisadas * 100, 1)
                        : 0
                }
            };

            _logger.LogInformation("Resumo de tempo de execução obtido com sucesso");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter resumo de tempo de execução");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpGet("dashboard-performance")]
    public async Task<ActionResult<object>> ObterDashboardPerformance()
    {
        _logger.LogInformation("ObterDashboardPerformance endpoint chamado");
        try
        {
            var resultado = await _ordemServicoService.ObterResumoTempoExecucaoAsync();

            var dashboard = new
            {
                metricas = new
                {
                    tempoMedioGeral = new
                    {
                        horas = Math.Round(resultado.EstatisticasGerais.TempoMedioEntregaHoras, 1),
                        formatado = resultado.EstatisticasGerais.TempoMedioFormatado,
                        performance = resultado.EstatisticasGerais.PerformanceGeral
                    },
                    produtividade = new
                    {
                        totalServicosConcluidos = resultado.EstatisticasGerais.TotalOrdensEntregues,
                        servicosEmAndamento = resultado.EstatisticasGerais.TotalOrdensAnalisadas - resultado.EstatisticasGerais.TotalOrdensEntregues,
                        taxaConclusao = resultado.EstatisticasGerais.TotalOrdensAnalisadas > 0
                            ? Math.Round((double)resultado.EstatisticasGerais.TotalOrdensEntregues / resultado.EstatisticasGerais.TotalOrdensAnalisadas * 100, 1)
                            : 0
                    }
                },

                clientesMaisEficientes = resultado.TempoMedioPorCliente
                    .Take(5)
                    .Select(c => new
                    {
                        nome = c.NomeCliente,
                        tempoMedio = c.TempoMedioFormatado,
                        totalServicos = c.TotalOrdensEntregues,
                        badge = c.Performance.Split(' ')[0] // Pega apenas o emoji
                    }),

                alertas = new
                {
                    servicosLentos = resultado.OrdensServico.Count(o => (o.TempoEntregaHoras ?? 0) > 120),
                    clientesComProblemas = resultado.TempoMedioPorCliente.Count(c => c.TempoMedioHoras > 120),
                    oportunidadeMelhoria = resultado.EstatisticasGerais.TempoMedioEntregaHoras > 72
                        ? "⚠️ Tempo m�dio acima de 3 dias - revisar processos"
                        : "✅ Tempo m�dio dentro do esperado"
                },

                resumo = resultado.EstatisticasGerais.ResumoExecutivo
            };

            _logger.LogInformation("Dashboard de performance obtido com sucesso");
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter dashboard de performance");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpGet("tempo-medio-execucao/simples")]
    public async Task<ActionResult<object>> ObterTempoMedioExecucaoSimples()
    {
        _logger.LogInformation("ObterTempoMedioExecucaoSimples endpoint chamado");
        try
        {
            var resumo = await _ordemServicoService.ObterResumoTempoExecucaoAsync();

            return Ok(new
            {
                // Dados principais
                tempoMedioHoras = Math.Round(resumo.EstatisticasGerais.TempoMedioEntregaHoras, 1),
                tempoMedioFormatado = resumo.EstatisticasGerais.TempoMedioFormatado,
                totalServicosConcluidos = resumo.EstatisticasGerais.TotalOrdensEntregues,
                totalOrdens = resumo.EstatisticasGerais.TotalOrdensAnalisadas,

                // Performance
                performance = resumo.EstatisticasGerais.PerformanceGeral,
                eficiencia = resumo.EstatisticasGerais.TotalOrdensAnalisadas > 0
                    ? $"{Math.Round((double)resumo.EstatisticasGerais.TotalOrdensEntregues / resumo.EstatisticasGerais.TotalOrdensAnalisadas * 100, 1)}%"
                    : "0%",

                // Resumo
                mensagem = resumo.EstatisticasGerais.ResumoExecutivo,

                // Sugest͵es
                dicas = new[]
                {
                resumo.EstatisticasGerais.TempoMedioEntregaHoras <= 24
                    ? "Excelente! Continuem assim!"
                    : "Considere otimizar os processos para reduzir o tempo m�dio",
                "Use 'dashboard-performance' para an�lise detalhada",
                "Verifique 'performance-por-cliente' para insights espec�ficos"
            }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter tempo médio de execução simples");
            return StatusCode(500, new { message = "Erro interno do servidor", details = ex.Message });
        }
    }

    [HttpPost("teste-monitoramento")]
    public IActionResult TesteMonitoramento([FromQuery] bool gerarErro = false)
    {
        _logger.LogInformation("TesteMonitoramento endpoint chamado. gerarErro={GerarErro}", gerarErro);
        if (gerarErro)
        {
            try
            {
                throw new InvalidOperationException("Simulação de erro para monitoramento Datadog");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro simulado no endpoint de teste de monitoramento");
                return StatusCode(500, new { message = "Erro simulado para monitoramento", details = ex.Message });
            }
        }
        _logger.LogInformation("Fluxo de sucesso simulado no endpoint de teste de monitoramento");
        return Ok(new { message = "Fluxo de sucesso simulado para monitoramento" });
    }


}