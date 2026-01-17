using OficinaCardozo.Application.DTOs;
using OficinaCardozo.Domain.Entities;

using OficinaCardozo.Domain.Interfaces;
using StatsdClient;

namespace OficinaCardozo.Application.Services;

public interface IOrdemServicoService
{
    Task<OrdemServicoDto> CreateOrdemServicoComOrcamentoAsync(CreateOrdemServicoDto createDto);
    Task<OrdemServicoDto?> GetByIdAsync(int id);
    Task<IEnumerable<OrdemServicoDto>> GetAllAsync();
    Task<IEnumerable<OrdemServicoDto>> GetAllAtivasAsync();
    Task<IEnumerable<OrcamentoDto>> GetAllOrcamentosAsync();
    Task<OrcamentoResumoDto> IniciarDiagnosticoAsync(int ordemServicoId);
    Task<OrcamentoResumoDto> FinalizarDiagnosticoAsync(int ordemServicoId);
    Task<OrcamentoResumoDto> EnviarOrcamentoParaAprovacaoAsync(EnviarOrcamentoParaAprovacaoDto enviarDto);
    Task<OrcamentoResumoDto> AprovarOrcamentoAsync(AprovarOrcamentoDto aprovarDto);
    Task<OrdemServicoDto> IniciarExecucaoAsync(int ordemServicoId);
    Task<OrdemServicoDto> FinalizarServicoAsync(int ordemServicoId);
    Task<OrdemServicoDto> EntregarVeiculoAsync(int ordemServicoId);

    Task<OrdemServicoDto> CancelarOrdemServicoAsync(CancelarOrdemServicoDto cancelarDto);
    Task<OrdemServicoDto> DevolverVeiculoSemServicoAsync(int ordemServicoId, string motivo);

    Task<string> DeletarTodosOrcamentosAsync();
    Task<string> DeletarOrcamentoPorIdAsync(int orcamentoId);

    Task<string> DeletarTodasOrdensServicoAsync();
    Task<string> DeletarOrdemServicoPorIdAsync(int ordemServicoId);

    Task<TempoMedioExecucaoDto> ObterTempoMedioExecucaoAsync(FiltroTempoMedioDto? filtro = null);

    Task<ResumoTempoExecucaoDto> ObterResumoTempoExecucaoAsync();
}

public class OrdemServicoService : IOrdemServicoService
{
    private static bool _metricsConfigured = false;
    private readonly IOrdemServicoRepository _ordemServicoRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IVeiculoRepository _veiculoRepository;
    private readonly IServicoRepository _servicoRepository;
    private readonly IPecaRepository _pecaRepository;
    private readonly IOrcamentoRepository _orcamentoRepository;
    private readonly IOrdemServicoStatusRepository _ordemServicoStatusRepository;
    private readonly IOrcamentoStatusRepository _orcamentoStatusRepository;

    public OrdemServicoService(
        IOrdemServicoRepository ordemServicoRepository,
        IClienteRepository clienteRepository,
        IVeiculoRepository veiculoRepository,
        IServicoRepository servicoRepository,
        IPecaRepository pecaRepository,
        IOrcamentoRepository orcamentoRepository,
        IOrdemServicoStatusRepository ordemServicoStatusRepository,
        IOrcamentoStatusRepository orcamentoStatusRepository)
    {
        _ordemServicoRepository = ordemServicoRepository;
        _clienteRepository = clienteRepository;
        _veiculoRepository = veiculoRepository;
        _servicoRepository = servicoRepository;
        _pecaRepository = pecaRepository;
        _orcamentoRepository = orcamentoRepository;
        _ordemServicoStatusRepository = ordemServicoStatusRepository;
        _orcamentoStatusRepository = orcamentoStatusRepository;
        // Configura StatsdClient.Metrics apenas uma vez
        if (!_metricsConfigured)
        {
            StatsdClient.Metrics.Configure(new StatsdClient.MetricsConfig
            {
                StatsdServerName = "localhost", // ajuste para o endereço do agente Datadog
                StatsdServerPort = 8125
            });
            _metricsConfigured = true;
        }
    }

    public async Task<OrdemServicoDto> CreateOrdemServicoComOrcamentoAsync(CreateOrdemServicoDto createDto)
    {
        var cliente = await _clienteRepository.GetByCpfCnpjAsync(createDto.ClienteCpfCnpj);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        if (cliente == null)
            throw new KeyNotFoundException($"Cliente com CPF/CNPJ {createDto.ClienteCpfCnpj} não encontrado");

        var veiculo = await _veiculoRepository.GetByPlacaAsync(createDto.VeiculoPlaca);
        if (veiculo == null)
        {
            veiculo = new Veiculo
            {
                IdCliente = cliente.Id,
                Placa = createDto.VeiculoPlaca,
                MarcaModelo = createDto.VeiculoMarcaModelo,
                AnoFabricacao = createDto.VeiculoAnoFabricacao,
                Cor = createDto.VeiculoCor,
                TipoCombustivel = createDto.VeiculoTipoCombustivel ?? "não informado"
            };
            veiculo = await _veiculoRepository.CreateAsync(veiculo);
        }
        else if (veiculo.IdCliente != cliente.Id)
        {
            throw new InvalidOperationException("Veículo pertence a outro cliente");
        }
        var servicos = new List<Servico>();
        foreach (var servicoId in createDto.ServicosIds)
        {
            var servico = await _servicoRepository.GetByIdAsync(servicoId);
            if (servico == null)
                throw new KeyNotFoundException($"Serviço com ID {servicoId} não encontrado");
            servicos.Add(servico);
        }

        var pecas = new List<Peca>();
        if (createDto.Pecas?.Any() == true)
        {
            foreach (var pecaDto in createDto.Pecas)
            {
                var peca = await _pecaRepository.GetByIdAsync(pecaDto.IdPeca);
                if (peca == null)
                    throw new KeyNotFoundException($"Peças com ID {pecaDto.IdPeca} não encontrada");

                if (peca.QuantidadeEstoque < pecaDto.Quantidade)
                    throw new InvalidOperationException($"Estoque insuficiente para a Peças {peca.NomePeca}. Disponível: {peca.QuantidadeEstoque}, Solicitado: {pecaDto.Quantidade}");

                pecas.Add(peca);
            }
        }

        var statusRecebida = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.RECEBIDA);
        if (statusRecebida == null)
            throw new InvalidOperationException("Status 'Recebida' não encontrado");

        var ordemServico = new OrdemServico
        {
            DataSolicitacao = DateTime.Now,
            IdVeiculo = veiculo.Id,
            IdStatus = statusRecebida.Id
        };

        var createdOrdemServico = await _ordemServicoRepository.CreateAsync(ordemServico);

        for (int i = 0; i < createDto.ServicosIds.Count; i++)
        {
            var servicoId = createDto.ServicosIds[i];
            var servico = servicos[i];

            var ordemServicoServico = new OrdemServicoServico
            {
                IdOrdemServico = createdOrdemServico.Id,
                IdServico = servicoId,
                ValorAplicado = servico.Preco
            };

            await _ordemServicoRepository.AddOrdemServicoServicoAsync(ordemServicoServico);
        }

        if (createDto.Pecas?.Any() == true)
        {
            for (int i = 0; i < createDto.Pecas.Count; i++)
            {
                var pecaDto = createDto.Pecas[i];
                var peca = pecas[i];

                var ordemServicoPeca = new OrdemServicoPeca
                {
                    IdOrdemServico = createdOrdemServico.Id,
                    IdPeca = pecaDto.IdPeca,
                    Quantidade = pecaDto.Quantidade,
                    ValorUnitario = peca.Preco
                };

                await _ordemServicoRepository.AddOrdemServicoPecaAsync(ordemServicoPeca);
            }
        }

         var result = await GetByIdAsync(createdOrdemServico.Id)
             ?? throw new InvalidOperationException("Erro ao recuperar ordem de Serviço criada com sucesso.");

         stopwatch.Stop();
         // Métricas customizadas Datadog
         StatsdClient.Metrics.Counter("oficinacardozo.ordem_servico.criada", 1);
         StatsdClient.Metrics.Timer("oficinacardozo.ordem_servico.tempo_criacao_ms", (int)stopwatch.ElapsedMilliseconds);

         return result;
    }

    public async Task<IEnumerable<OrdemServicoDto>> GetAllAtivasAsync()
    {
        var ordensServico = await _ordemServicoRepository.GetAllWithDetailsAsync();

        var statusExcluidos = new[] {
        OrdemServicoStatusConstants.FINALIZADA,
        OrdemServicoStatusConstants.ENTREGUE
        };

        var ordensAtivas = ordensServico
            .Where(os => !statusExcluidos.Contains(os.Status?.Descricao))
            .ToList();

        var prioridadeStatus = new Dictionary<string, int>
        {
            { OrdemServicoStatusConstants.EM_EXECUCAO, 1 },
            { OrdemServicoStatusConstants.AGUARDANDO_APROVACAO, 2 },
            { OrdemServicoStatusConstants.EM_DIAGNOSTICO, 3 },
            { OrdemServicoStatusConstants.RECEBIDA, 4 },
            { OrdemServicoStatusConstants.EM_ELABORACAO, 5 },
            { OrdemServicoStatusConstants.CANCELADA, 6 },
            { OrdemServicoStatusConstants.DEVOLVIDA, 7 }
        };

        var ordensOrdenadas = ordensAtivas
            .OrderBy(os => prioridadeStatus.GetValueOrDefault(os.Status?.Descricao ?? "", 999))
            .ThenBy(os => os.DataSolicitacao)
            .ToList();

        return ordensOrdenadas.Select(MapToDto);
    }

    public async Task<OrcamentoResumoDto> IniciarDiagnosticoAsync(int ordemServicoId)
    {
        
        var ordemServicoDetails = await _ordemServicoRepository.GetByIdWithDetailsAsync(ordemServicoId);
        if (ordemServicoDetails == null)
            throw new KeyNotFoundException("Ordem de serviço não encontrada");

        
        var statusRecebida = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.RECEBIDA);
        if (statusRecebida == null)
            throw new InvalidOperationException("Status 'Recebida' não encontrado");

        if (ordemServicoDetails.IdStatus != statusRecebida.Id)
            throw new InvalidOperationException($"Ordem de serviço deve estar no status 'Recebida' para iniciar diagnóstico. Status atual: {ordemServicoDetails.Status?.Descricao}");

        
        var statusEmDiagnostico = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.EM_DIAGNOSTICO);
        if (statusEmDiagnostico == null)
            throw new InvalidOperationException("Status 'Em diagnostico' não encontrado");

        
        var ordemServicoParaUpdate = await _ordemServicoRepository.GetByIdAsync(ordemServicoId);
        if (ordemServicoParaUpdate == null)
            throw new KeyNotFoundException("Ordem de serviço não encontrada para atualização");

       
        ordemServicoParaUpdate.IdStatus = statusEmDiagnostico.Id;
        await _ordemServicoRepository.UpdateAsync(ordemServicoParaUpdate);

        
        var orcamento = await GerarOrcamentoAsync(ordemServicoId);

        return new OrcamentoResumoDto
        {
            Id = orcamento.Id,
            DataOrcamento = orcamento.DataOrcamento,
            StatusDescricao = "Diagnostico iniciado",
            ClienteNome = ordemServicoDetails.Veiculo?.Cliente?.Nome ?? "",
            ClienteEmail = ordemServicoDetails.Veiculo?.Cliente?.EmailPrincipal ?? "",
            VeiculoPlaca = ordemServicoDetails.Veiculo?.Placa ?? "",
            VeiculoMarcaModelo = ordemServicoDetails.Veiculo?.MarcaModelo ?? "",
            ValorTotal = 0,
            MensagemAprovacao = "Diagnostico iniciado. Orçamento Em elaboracao."
        };
    }

    public async Task<OrcamentoResumoDto> FinalizarDiagnosticoAsync(int ordemServicoId)
    {
      
        var ordemServicoDetails = await _ordemServicoRepository.GetByIdWithDetailsAsync(ordemServicoId);
        if (ordemServicoDetails == null)
            throw new KeyNotFoundException("Ordem de serviço não encontrada");

 
        var statusEmDiagnostico = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.EM_DIAGNOSTICO);
        if (statusEmDiagnostico == null)
            throw new InvalidOperationException("Status 'Em diagnostico' não encontrado");

        if (ordemServicoDetails.IdStatus != statusEmDiagnostico.Id)
            throw new InvalidOperationException($"Ordem de serviço deve estar 'Em diagnostico' para finalizar. Status atual: {ordemServicoDetails.Status?.Descricao}");

        
        var orcamento = ordemServicoDetails.Orcamentos?.FirstOrDefault();
        if (orcamento == null)
            throw new InvalidOperationException("Nenhum orçamento encontrado para esta ordem de serviço");

        
        var statusOrcamentoCriado = await _orcamentoStatusRepository.GetByDescricaoAsync(OrcamentoStatusConstants.CRIADO);
        if (orcamento.IdStatus != statusOrcamentoCriado?.Id)
            throw new InvalidOperationException("Orçamento deve estar no status 'Criado' para finalizar diagnóstico");

       
        var statusEmElaboracao = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.EM_ELABORACAO);
        if (statusEmElaboracao == null)
            throw new InvalidOperationException("Status 'Em elaboracao' para ordem de serviço não encontrado");

        var ordemServicoParaUpdate = await _ordemServicoRepository.GetByIdAsync(ordemServicoId);
        if (ordemServicoParaUpdate == null)
            throw new KeyNotFoundException("Ordem de serviço não encontrada para atualização");

        ordemServicoParaUpdate.IdStatus = statusEmElaboracao.Id;
        await _ordemServicoRepository.UpdateAsync(ordemServicoParaUpdate);

        var statusOrcamentoEmElaboracao = await _orcamentoStatusRepository.GetByDescricaoAsync(OrcamentoStatusConstants.EM_ELABORACAO);
        if (statusOrcamentoEmElaboracao != null)
        {
            orcamento.IdStatus = statusOrcamentoEmElaboracao.Id;
            await _orcamentoRepository.UpdateAsync(orcamento);
        }

        return new OrcamentoResumoDto
        {
            Id = orcamento.Id,
            DataOrcamento = orcamento.DataOrcamento,
            StatusDescricao = "Orçamento Em elaboracao - Pronto para envio",
            ClienteNome = ordemServicoDetails.Veiculo?.Cliente?.Nome ?? "",
            ClienteEmail = ordemServicoDetails.Veiculo?.Cliente?.EmailPrincipal ?? "",
            VeiculoPlaca = ordemServicoDetails.Veiculo?.Placa ?? "",
            VeiculoMarcaModelo = ordemServicoDetails.Veiculo?.MarcaModelo ?? "",
            ValorTotal = CalcularValorTotalOrcamento(orcamento),
            MensagemAprovacao = "Diagnóstico finalizado! Orçamento elaborado e pronto para ser enviado ao cliente."
        };
    }
    public async Task<OrcamentoResumoDto> EnviarOrcamentoParaAprovacaoAsync(EnviarOrcamentoParaAprovacaoDto enviarDto)
    {
        var orcamento = await _orcamentoRepository.GetByIdWithDetailsAsync(enviarDto.IdOrcamento);
        if (orcamento == null)
            throw new KeyNotFoundException("Orçamento não encontrado");

        var statusOrcamentoValidos = new[] {
            OrcamentoStatusConstants.CRIADO,
            OrcamentoStatusConstants.EM_ELABORACAO
        };

        var statusOrcamentoAtual = orcamento.Status?.Descricao;
        if (!statusOrcamentoValidos.Contains(statusOrcamentoAtual))
            throw new InvalidOperationException($"Orçamento deve estar 'Criado' ou 'Em elaboracao' para envio. Status atual: '{statusOrcamentoAtual}'");

        var statusEmElaboracao = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.EM_ELABORACAO);
        if (orcamento.OrdemServico?.IdStatus != statusEmElaboracao?.Id)
            throw new InvalidOperationException("Ordem de serviço deve estar 'Em elaboracao' para envio");

       var statusAguardandoAprovacao = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.AGUARDANDO_APROVACAO);
        if (statusAguardandoAprovacao == null)
            throw new InvalidOperationException("Status 'Aguardando aprovaçao' não encontrado");

        var ordemServico = await _ordemServicoRepository.GetByIdAsync(orcamento.IdOrdemServico);
        if (ordemServico != null)
        {
            ordemServico.IdStatus = statusAguardandoAprovacao.Id;
            await _ordemServicoRepository.UpdateAsync(ordemServico);
        }

        var statusOrcamentoPendente = await _orcamentoStatusRepository.GetByDescricaoAsync(OrcamentoStatusConstants.PENDENTE_APROVACAO);
        if (statusOrcamentoPendente == null)
            throw new InvalidOperationException("Status 'Pendente aprovacao' para orçamento não encontrado");

        orcamento.IdStatus = statusOrcamentoPendente.Id;
        await _orcamentoRepository.UpdateAsync(orcamento);

        return new OrcamentoResumoDto
        {
            Id = orcamento.Id,
            DataOrcamento = orcamento.DataOrcamento,
            StatusDescricao = OrcamentoStatusConstants.PENDENTE_APROVACAO,
            ClienteNome = orcamento.OrdemServico?.Veiculo?.Cliente?.Nome ?? "",
            ClienteEmail = orcamento.OrdemServico?.Veiculo?.Cliente?.EmailPrincipal ?? "",
            VeiculoPlaca = orcamento.OrdemServico?.Veiculo?.Placa ?? "",
            VeiculoMarcaModelo = orcamento.OrdemServico?.Veiculo?.MarcaModelo ?? "",
            ValorTotal = CalcularValorTotalOrcamento(orcamento),
            MensagemAprovacao = $"Orçamento enviado para aprovação do cliente. {enviarDto.Observacoes ?? ""}"
        };
    }

    public async Task<OrcamentoResumoDto> AprovarOrcamentoAsync(AprovarOrcamentoDto aprovarDto)
    {
        var orcamento = await _orcamentoRepository.GetByIdWithDetailsAsync(aprovarDto.IdOrcamento);
        if (orcamento == null)
            throw new KeyNotFoundException("Orçamento não encontrado");

        var statusOrcamentoAtual = orcamento.Status?.Descricao;

        if (statusOrcamentoAtual != OrcamentoStatusConstants.PENDENTE_APROVACAO)
            throw new InvalidOperationException($"Orçamento não pode ser processado. Status atual: '{statusOrcamentoAtual}'. Apenas orçamentos 'Pendente aprovacao' podem ser aprovados/rejeitados.");

        var statusAguardandoAprovacao = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.AGUARDANDO_APROVACAO);
        if (orcamento.OrdemServico?.IdStatus != statusAguardandoAprovacao?.Id)
            throw new InvalidOperationException("Ordem de serviço deve estar 'Aguardando aprovacao' para processar resposta do cliente");

        string statusOrdemServicoDescricao;
        string statusOrcamentoDescricao;
        string mensagemFinal;
        Orcamento? novoOrcamento = null;

        if (aprovarDto.Aprovado)
        {
            statusOrdemServicoDescricao = OrdemServicoStatusConstants.EM_EXECUCAO;
            statusOrcamentoDescricao = OrcamentoStatusConstants.APROVADO;
            mensagemFinal = "Orçamento aprovado! Serviço iniciado automaticamente.";
        }
        else
        {
            if (aprovarDto.SolicitarNovoOrcamento)
            {
                statusOrdemServicoDescricao = OrdemServicoStatusConstants.EM_ELABORACAO;
                statusOrcamentoDescricao = OrcamentoStatusConstants.REJEITADO;

                novoOrcamento = await GerarOrcamentoAsync(orcamento.IdOrdemServico);

                mensagemFinal = $"Orçamento rejeitado. Novo orçamento criado (ID: {novoOrcamento.Id}) - oficina irá elaborar nova proposta.";
            }
            else
            {
                if (aprovarDto.VeiculoJaRetirado)
                {
                    statusOrdemServicoDescricao = OrdemServicoStatusConstants.DEVOLVIDA;
                    mensagemFinal = "Orçamento rejeitado e veículo já foi devolvido. Ordem finalizada.";
                }
                else
                {
                    statusOrdemServicoDescricao = OrdemServicoStatusConstants.CANCELADA;
                    mensagemFinal = "Orçamento rejeitado e serviço cancelado. Aguardando retirada do veículo.";
                }
                statusOrcamentoDescricao = OrcamentoStatusConstants.REJEITADO;
            }
        }

        await AtualizarStatusOrdemServicoAsync(orcamento.IdOrdemServico, statusOrdemServicoDescricao);
        await AtualizarStatusOrcamentoAsync(orcamento, statusOrcamentoDescricao);

        var mensagemCompleta = ConstruirMensagemCompleta(mensagemFinal, aprovarDto);

        var orcamentoParaResposta = novoOrcamento ?? orcamento;
        var statusParaResposta = novoOrcamento != null ? "Criado" : statusOrcamentoDescricao;

        return new OrcamentoResumoDto
        {
            Id = orcamentoParaResposta.Id,
            DataOrcamento = orcamentoParaResposta.DataOrcamento,
            StatusDescricao = statusParaResposta,
            ClienteNome = orcamento.OrdemServico?.Veiculo?.Cliente?.Nome ?? "",
            ClienteEmail = orcamento.OrdemServico?.Veiculo?.Cliente?.EmailPrincipal ?? "",
            VeiculoPlaca = orcamento.OrdemServico?.Veiculo?.Placa ?? "",
            VeiculoMarcaModelo = orcamento.OrdemServico?.Veiculo?.MarcaModelo ?? "",
            ValorTotal = CalcularValorTotalOrcamento(orcamento),
            MensagemAprovacao = mensagemCompleta
        };
    }

    private async Task AtualizarStatusOrdemServicoAsync(int idOrdemServico, string statusDescricao)
    {
        var statusOrdemServico = await _ordemServicoStatusRepository.GetByDescricaoAsync(statusDescricao);
        if (statusOrdemServico == null)
            throw new InvalidOperationException($"Status '{statusDescricao}' para ordem de serviço não encontrado");

        var ordemServico = await _ordemServicoRepository.GetByIdAsync(idOrdemServico);
        if (ordemServico != null)
        {
            ordemServico.IdStatus = statusOrdemServico.Id;
            await _ordemServicoRepository.UpdateAsync(ordemServico);
        }
    }

    private async Task AtualizarStatusOrcamentoAsync(Orcamento orcamento, string statusDescricao)
    {
        var statusOrcamento = await _orcamentoStatusRepository.GetByDescricaoAsync(statusDescricao);
        if (statusOrcamento == null)
            throw new InvalidOperationException($"Status '{statusDescricao}' para orçamento não encontrado");

        orcamento.IdStatus = statusOrcamento.Id;
        await _orcamentoRepository.UpdateAsync(orcamento);
    }

    private static string ConstruirMensagemCompleta(string mensagemFinal, AprovarOrcamentoDto aprovarDto)
    {
        var mensagemCompleta = mensagemFinal;

        if (!string.IsNullOrWhiteSpace(aprovarDto.MotivoRejeicao))
        {
            mensagemCompleta += $"\n\nMotivo: {aprovarDto.MotivoRejeicao}";
        }

        if (!string.IsNullOrWhiteSpace(aprovarDto.Observacoes))
        {
            mensagemCompleta += $"\nObservações: {aprovarDto.Observacoes}";
        }

        return mensagemCompleta;
    }

    public async Task<OrdemServicoDto> IniciarExecucaoAsync(int ordemServicoId)
    {
        var ordemServico = await _ordemServicoRepository.GetByIdWithDetailsAsync(ordemServicoId);
        if (ordemServico == null)
            throw new KeyNotFoundException("Ordem de serviço não encontrada");

        var statusEmExecucao = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.EM_EXECUCAO);
        if (ordemServico.IdStatus == statusEmExecucao?.Id)
            return MapToDto(ordemServico); 

        throw new InvalidOperationException("Ordem de Serviço deve estar 'Em execucao' aprovação do orçamento");
    }

    public async Task<OrdemServicoDto> FinalizarServicoAsync(int ordemServicoId)
    {
        var ordemServicoDetails = await _ordemServicoRepository.GetByIdWithDetailsAsync(ordemServicoId);
        if (ordemServicoDetails == null)
            throw new KeyNotFoundException("Ordem de serviço não encontrada");

        var statusEmExecucao = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.EM_EXECUCAO);
        if (statusEmExecucao == null)
            throw new InvalidOperationException("Status 'Em execucao' não encontrado");

        if (ordemServicoDetails.IdStatus != statusEmExecucao.Id)
            throw new InvalidOperationException($"Ordem de serviço deve estar 'Em execucao' para ser finalizada. Status atual: {ordemServicoDetails.Status?.Descricao}");

        var statusFinalizada = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.FINALIZADA);
        if (statusFinalizada == null)
            throw new InvalidOperationException("Status 'Finalizada' não encontrado");

        var ordemServicoParaUpdate = await _ordemServicoRepository.GetByIdAsync(ordemServicoId);
        if (ordemServicoParaUpdate == null)
            throw new KeyNotFoundException("Ordem de serviço não encontrada para atualização");

        ordemServicoParaUpdate.IdStatus = statusFinalizada.Id;
        ordemServicoParaUpdate.DataFinalizacao = DateTime.Now;
        await _ordemServicoRepository.UpdateAsync(ordemServicoParaUpdate);

        var ordemAtualizada = await _ordemServicoRepository.GetByIdWithDetailsAsync(ordemServicoId);
        return MapToDto(ordemAtualizada!);
    }
    public async Task<OrdemServicoDto> EntregarVeiculoAsync(int ordemServicoId)
    {
        var ordemServicoDetails = await _ordemServicoRepository.GetByIdWithDetailsAsync(ordemServicoId);
        if (ordemServicoDetails == null)
            throw new KeyNotFoundException("Ordem de serviço não encontrada");

        var statusFinalizada = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.FINALIZADA);
        if (statusFinalizada == null)
            throw new InvalidOperationException("Status 'Finalizada' não encontrado");

        if (ordemServicoDetails.IdStatus != statusFinalizada.Id)
            throw new InvalidOperationException($"Ordem de serviço deve estar 'Finalizada' para ser entregue. Status atual: {ordemServicoDetails.Status?.Descricao}");

        var statusEntregue = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.ENTREGUE);
        if (statusEntregue == null)
            throw new InvalidOperationException("Status 'Entregue' não encontrado");

        var ordemServicoParaUpdate = await _ordemServicoRepository.GetByIdAsync(ordemServicoId);
        if (ordemServicoParaUpdate == null)
            throw new KeyNotFoundException("Ordem de serviço não encontrada para atualização");

        ordemServicoParaUpdate.IdStatus = statusEntregue.Id;
        ordemServicoParaUpdate.DataEntrega = DateTime.Now;
        await _ordemServicoRepository.UpdateAsync(ordemServicoParaUpdate);

        var ordemAtualizada = await _ordemServicoRepository.GetByIdWithDetailsAsync(ordemServicoId);
        return MapToDto(ordemAtualizada!);
    }

    public async Task<OrdemServicoDto?> GetByIdAsync(int id)
    {
        var ordemServico = await _ordemServicoRepository.GetByIdWithDetailsAsync(id);
        return ordemServico != null ? MapToDto(ordemServico) : null;
    }

    public async Task<IEnumerable<OrdemServicoDto>> GetAllAsync()
    {
        var ordensServico = await _ordemServicoRepository.GetAllWithDetailsAsync();
        return ordensServico.Select(MapToDto);
    }

    public async Task<IEnumerable<OrcamentoDto>> GetAllOrcamentosAsync()
    {
        var orcamentos = await _orcamentoRepository.GetAllWithDetailsAsync();
        return orcamentos.Select(MapOrcamentoToDto);
    }

    public async Task<string> DeletarTodosOrcamentosAsync()
    {
        try
        {
            var totalExcluido = await _orcamentoRepository.DeleteAllAsync();
            return $"Sucesso: {totalExcluido} orçamento(s) excluído(s) do banco de dados.";
        }
        catch (Exception ex)
        {
            return $"Erro ao excluir orçamentos: {ex.Message}";
        }
    }

    public async Task<string> DeletarOrcamentoPorIdAsync(int orcamentoId)
    {
        try
        {
            var orcamento = await _orcamentoRepository.GetByIdWithDetailsAsync(orcamentoId);
            if (orcamento == null)
                return $"Orçamento com ID {orcamentoId} não encontrado.";

            var clienteNome = orcamento.OrdemServico?.Veiculo?.Cliente?.Nome ?? "não identificado";
            var placa = orcamento.OrdemServico?.Veiculo?.Placa ?? "N/A";

            var excluido = await _orcamentoRepository.DeleteAsync(orcamentoId);

            if (excluido)
                return $"Sucesso: orçamento ID {orcamentoId} excluído (Cliente: {clienteNome}, Placa: {placa}).";
            else
                return $"Falha ao excluir orçamento ID {orcamentoId}.";
        }
        catch (Exception ex)
        {
            return $"Erro ao excluir orçamento ID {orcamentoId}: {ex.Message}";
        }
    }

    public async Task<string> DeletarTodasOrdensServicoAsync()
    {
        try
        {
            var totalExcluido = await _ordemServicoRepository.DeleteAllAsync();
            return $"Sucesso: {totalExcluido} ordem(ns) de Serviço excluída(s) do banco de dados (incluindo orçamentos e relacionamentos).";
        }
        catch (Exception ex)
        {
            return $"Erro ao excluir ordens de Serviço: {ex.Message}";
        }
    }

    public async Task<string> DeletarOrdemServicoPorIdAsync(int ordemServicoId)
    {
        try
        {
            var ordemServico = await _ordemServicoRepository.GetByIdWithDetailsAsync(ordemServicoId);
            if (ordemServico == null)
                return $"Ordem de Serviço com ID {ordemServicoId} não encontrada.";

            var clienteNome = ordemServico.Veiculo?.Cliente?.Nome ?? "não identificado";
            var placa = ordemServico.Veiculo?.Placa ?? "N/A";
            var dataServico = ordemServico.DataSolicitacao.ToString("dd/MM/yyyy");
            var statusDescricao = ordemServico.Status?.Descricao ?? "Status não identificado";

            var excluido = await _ordemServicoRepository.DeleteAsync(ordemServicoId);

            if (excluido)
                return $"Sucesso: Ordem de Serviço ID {ordemServicoId} excluída (Cliente: {clienteNome}, Placa: {placa}, Data: {dataServico}, Status: {statusDescricao}).";
            else
                return $"Falha ao excluir ordem de Serviço ID {ordemServicoId}.";
        }
        catch (Exception ex)
        {
            return $"Erro ao excluir ordem de Serviço ID {ordemServicoId}: {ex.Message}";
        }
    }

    public async Task<OrdemServicoDto> CancelarOrdemServicoAsync(CancelarOrdemServicoDto cancelarDto)
    {
        var ordemServicoDetails = await _ordemServicoRepository.GetByIdWithDetailsAsync(cancelarDto.IdOrdemServico);
        if (ordemServicoDetails == null)
            throw new KeyNotFoundException("Ordem de Serviço não encontrada");

        var statusPermitidos = new[] {
        OrdemServicoStatusConstants.RECEBIDA,
        OrdemServicoStatusConstants.EM_DIAGNOSTICO,
        OrdemServicoStatusConstants.EM_ELABORACAO,
        OrdemServicoStatusConstants.AGUARDANDO_APROVACAO
    };

        var statusAtual = ordemServicoDetails.Status?.Descricao ?? "";
        if (!statusPermitidos.Contains(statusAtual))
            throw new InvalidOperationException($"Ordem de Serviço não pode ser cancelada no status '{statusAtual}'");

        var statusCancelada = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.CANCELADA);
        if (statusCancelada == null)
            throw new InvalidOperationException("Status 'Cancelada' não encontrado no banco");

        var ordemServicoParaUpdate = await _ordemServicoRepository.GetByIdAsync(cancelarDto.IdOrdemServico);
        if (ordemServicoParaUpdate == null)
            throw new KeyNotFoundException("Ordem de Serviço não encontrada para atualizaÍƒÂ§ÍƒÂ£o");

        ordemServicoParaUpdate.IdStatus = statusCancelada.Id;
        await _ordemServicoRepository.UpdateAsync(ordemServicoParaUpdate);

        var orcamento = ordemServicoDetails.Orcamentos?.FirstOrDefault();
        if (orcamento != null)
        {
            var statusRejeitado = await _orcamentoStatusRepository.GetByDescricaoAsync(OrcamentoStatusConstants.REJEITADO);
            if (statusRejeitado != null)
            {
                orcamento.IdStatus = statusRejeitado.Id;
                await _orcamentoRepository.UpdateAsync(orcamento);
            }
        }

        if (cancelarDto.VeiculoDevolvido)
        {
            var statusDevolvida = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.DEVOLVIDA);
            if (statusDevolvida != null)
            {
                ordemServicoParaUpdate.IdStatus = statusDevolvida.Id;
                await _ordemServicoRepository.UpdateAsync(ordemServicoParaUpdate);
            }
        }

        var ordemAtualizada = await _ordemServicoRepository.GetByIdWithDetailsAsync(cancelarDto.IdOrdemServico);
        return MapToDto(ordemAtualizada!);
    }

    public async Task<OrdemServicoDto> DevolverVeiculoSemServicoAsync(int ordemServicoId, string motivo)
    {
        var ordemServico = await _ordemServicoRepository.GetByIdWithDetailsAsync(ordemServicoId);
        if (ordemServico == null)
            throw new KeyNotFoundException("Ordem de Serviço não encontrada");

        var statusCancelada = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.CANCELADA);
        if (ordemServico.IdStatus != statusCancelada?.Id)
            throw new InvalidOperationException("Ordem de Serviço deve estar 'Cancelada' para devolver Veículo sem Serviço");

        var statusDevolvida = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.DEVOLVIDA);
        if (statusDevolvida == null)
            throw new InvalidOperationException("Status 'Devolvida' não encontrado");

        var ordemServicoParaUpdate = await _ordemServicoRepository.GetByIdAsync(ordemServicoId);
        if (ordemServicoParaUpdate == null)
            throw new KeyNotFoundException("Ordem de Serviço não encontrada para atualizaÍƒÂ§ÍƒÂ£o");

        ordemServicoParaUpdate.IdStatus = statusDevolvida.Id;
        ordemServicoParaUpdate.DataEntrega = DateTime.Now;
        await _ordemServicoRepository.UpdateAsync(ordemServicoParaUpdate);

        var ordemAtualizada = await _ordemServicoRepository.GetByIdWithDetailsAsync(ordemServicoId);
        return MapToDto(ordemAtualizada!);
    }

    public async Task<TempoMedioExecucaoDto> ObterTempoMedioExecucaoAsync(FiltroTempoMedioDto? filtro = null)
    {
        var dataFim = filtro?.DataFim ?? DateTime.Now;
        var dataInicio = filtro?.DataInicio ?? dataFim.AddDays(-30);

        // Buscar status "Entregue" - se não existir, retornar dados zerados
        var statusEntregue = await _ordemServicoStatusRepository.GetByDescricaoAsync(OrdemServicoStatusConstants.ENTREGUE);
        if (statusEntregue == null)
        {
            // Em vez de lançar exceção, retorna resultado vazio
            return new TempoMedioExecucaoDto
            {
                TempoMedioTotalDias = 0,
                TempoMedioTotalHoras = 0,
                TotalServicosConcluidos = 0,
                DataInicio = dataInicio,
                DataFim = dataFim,
                FasesDetalhadas = new FasesTempoMedioDto(),
                ServicoMaisRapido = new ServicoTempoDetalheDto(),
                ServicoMaisLento = new ServicoTempoDetalheDto()
            };
        }

        var ordensEntregues = await _ordemServicoRepository.GetAllWithDetailsAsync();

        var ordensFiltradasQuery = ordensEntregues
            .Where(os => os.IdStatus == statusEntregue.Id &&
                         os.DataSolicitacao >= dataInicio &&
                         os.DataSolicitacao <= dataFim &&
                         os.DataEntrega.HasValue);

        if (filtro?.IdCliente.HasValue == true)
        {
            ordensFiltradasQuery = ordensFiltradasQuery
                .Where(os => os.Veiculo?.IdCliente == filtro.IdCliente.Value);
        }

        if (filtro?.ValorMinimo.HasValue == true)
        {
            ordensFiltradasQuery = ordensFiltradasQuery
                .Where(os => CalcularValorTotalOrdemServico(os) >= filtro.ValorMinimo.Value);
        }

        var ordensFiltradas = ordensFiltradasQuery.ToList();

        if (!ordensFiltradas.Any())
        {
            return new TempoMedioExecucaoDto
            {
                TempoMedioTotalDias = 0,
                TempoMedioTotalHoras = 0,
                TotalServicosConcluidos = 0,
                DataInicio = dataInicio,
                DataFim = dataFim,
                FasesDetalhadas = new FasesTempoMedioDto(),
                ServicoMaisRapido = new ServicoTempoDetalheDto(),
                ServicoMaisLento = new ServicoTempoDetalheDto()
            };
        }

        var temposExecucao = new List<(OrdemServico ordem, TimeSpan tempoTotal)>();

        foreach (var ordem in ordensFiltradas)
        {
            if (ordem.DataEntrega.HasValue)
            {
                var tempoTotal = ordem.DataEntrega.Value - ordem.DataSolicitacao;

                if (tempoTotal.TotalHours > 0)
                {
                    temposExecucao.Add((ordem, tempoTotal));
                }
            }
        }

        if (!temposExecucao.Any())
        {
            return new TempoMedioExecucaoDto
            {
                TempoMedioTotalDias = 0,
                TempoMedioTotalHoras = 0,
                TotalServicosConcluidos = 0,
                DataInicio = dataInicio,
                DataFim = dataFim,
                FasesDetalhadas = new FasesTempoMedioDto(),
                ServicoMaisRapido = new ServicoTempoDetalheDto(),
                ServicoMaisLento = new ServicoTempoDetalheDto()
            };
        }

        var tempoMedioHoras = temposExecucao.Average(t => t.tempoTotal.TotalHours);
        var tempoMedioDias = tempoMedioHoras / 24;

        var servicoMaisRapido = temposExecucao.OrderBy(t => t.tempoTotal).First();
        var servicoMaisLento = temposExecucao.OrderByDescending(t => t.tempoTotal).First();

        var fasesDetalhadas = CalcularFasesDetalhadas(ordensFiltradas);

        return new TempoMedioExecucaoDto
        {
            TempoMedioTotalDias = Math.Round(tempoMedioDias, 2),
            TempoMedioTotalHoras = Math.Round(tempoMedioHoras, 2),
            TotalServicosConcluidos = temposExecucao.Count,
            DataInicio = dataInicio,
            DataFim = dataFim,
            FasesDetalhadas = fasesDetalhadas,
            ServicoMaisRapido = MapearServicoDetalheReal(servicoMaisRapido.ordem),
            ServicoMaisLento = MapearServicoDetalheReal(servicoMaisLento.ordem)
        };
    }


    private static decimal CalcularValorTotalOrdemServico(OrdemServico ordem)
    {
        var valorServicos = ordem.OrdemServicoServicos?.Sum(s => s.ValorAplicado ?? 0) ?? 0;
        var valorPecas = ordem.OrdemServicoPecas?.Sum(p => (p.ValorUnitario ?? 0) * p.Quantidade) ?? 0;
        return valorServicos + valorPecas;
    }

    private async Task<Orcamento> GerarOrcamentoAsync(int idOrdemServico)
    {
        var statusCriado = await _orcamentoStatusRepository.GetByDescricaoAsync(OrcamentoStatusConstants.CRIADO);
        if (statusCriado == null)
            throw new InvalidOperationException("Status 'Criado' para orçamento não encontrado");

        var orcamento = new Orcamento
        {
            DataOrcamento = DateTime.Now,
            IdOrdemServico = idOrdemServico,
            IdStatus = statusCriado.Id
        };

        return await _orcamentoRepository.CreateAsync(orcamento);
    }

    private static decimal CalcularValorTotalOrcamento(Orcamento orcamento)
    {
        decimal valorServicos = orcamento.OrdemServico?.OrdemServicoServicos
            ?.Sum(s => s.ValorAplicado ?? 0) ?? 0;

        decimal valorPecas = orcamento.OrdemServico?.OrdemServicoPecas
            ?.Sum(p => (p.ValorUnitario ?? 0) * p.Quantidade) ?? 0;

        return valorServicos + valorPecas;
    }

    private static OrcamentoDto MapOrcamentoToDto(Orcamento orcamento)
    {
        var valorServicos = orcamento.OrdemServico?.OrdemServicoServicos?.Sum(s => s.ValorAplicado ?? 0) ?? 0;
        var valorPecas = orcamento.OrdemServico?.OrdemServicoPecas?.Sum(p => (p.ValorUnitario ?? 0) * p.Quantidade) ?? 0;

        return new OrcamentoDto
        {
            Id = orcamento.Id,
            DataOrcamento = orcamento.DataOrcamento,
            IdOrdemServico = orcamento.IdOrdemServico,
            IdStatus = orcamento.IdStatus,
            StatusDescricao = orcamento.Status?.Descricao,

            ClienteNome = orcamento.OrdemServico?.Veiculo?.Cliente?.Nome,
            ClienteEmail = orcamento.OrdemServico?.Veiculo?.Cliente?.EmailPrincipal,
            VeiculoPlaca = orcamento.OrdemServico?.Veiculo?.Placa,
            VeiculoMarcaModelo = orcamento.OrdemServico?.Veiculo?.MarcaModelo,

            ValorServicos = valorServicos,
            ValorPecas = valorPecas,
            ValorTotal = valorServicos + valorPecas,

            Servicos = orcamento.OrdemServico?.OrdemServicoServicos?.Select(s => new OrcamentoServicoDto
            {
                NomeServico = s.Servico?.NomeServico ?? "",
                ValorAplicado = s.ValorAplicado ?? 0,
                TempoEstimado = s.Servico?.TempoEstimadoExecucao ?? 0,
                DescricaoDetalhada = s.Servico?.DescricaoDetalhadaServico ?? ""
            }).ToList() ?? new List<OrcamentoServicoDto>(),

            Pecas = orcamento.OrdemServico?.OrdemServicoPecas?.Select(p => new OrcamentoPecaDto
            {
                NomePeca = p.Peca?.NomePeca ?? "",
                CodigoIdentificador = p.Peca?.CodigoIdentificador ?? "",
                Quantidade = p.Quantidade,
                ValorUnitario = p.ValorUnitario ?? 0,
                ValorTotal = (p.ValorUnitario ?? 0) * p.Quantidade,
                UnidadeMedida = p.Peca?.UnidadeMedida ?? ""
            }).ToList() ?? new List<OrcamentoPecaDto>()
        };
    }

    private static OrdemServicoDto MapToDto(OrdemServico ordemServico)
    {
        return new OrdemServicoDto
        {
            Id = ordemServico.Id,
            DataSolicitacao = ordemServico.DataSolicitacao,
            IdVeiculo = ordemServico.IdVeiculo,
            IdStatus = ordemServico.IdStatus,
            StatusDescricao = ordemServico.Status?.Descricao,

            DataFinalizacao = ordemServico.DataFinalizacao,
            DataEntrega = ordemServico.DataEntrega,

            VeiculoPlaca = ordemServico.Veiculo?.Placa,
            VeiculoMarcaModelo = ordemServico.Veiculo?.MarcaModelo,
            VeiculoAnoFabricacao = ordemServico.Veiculo?.AnoFabricacao,
            VeiculoCor = ordemServico.Veiculo?.Cor,

            ClienteNome = ordemServico.Veiculo?.Cliente?.Nome,
            ClienteCpfCnpj = ordemServico.Veiculo?.Cliente?.CpfCnpj,
            ClienteEmail = ordemServico.Veiculo?.Cliente?.EmailPrincipal,
            ClienteTelefone = ordemServico.Veiculo?.Cliente?.TelefonePrincipal,

            Servicos = ordemServico.OrdemServicoServicos?.Select(s => new OrdemServicoServicoDto
            {
                IdServico = s.IdServico,
                NomeServico = s.Servico?.NomeServico,
                ValorAplicado = s.ValorAplicado,
                PrecoOriginal = s.Servico?.Preco,
                TempoEstimado = s.Servico?.TempoEstimadoExecucao,
                DescricaoDetalhada = s.Servico?.DescricaoDetalhadaServico
            }).ToList() ?? new List<OrdemServicoServicoDto>(),

            Pecas = ordemServico.OrdemServicoPecas?.Select(p => new OrdemServicoPecaDto
            {
                IdPeca = p.IdPeca,
                NomePeca = p.Peca?.NomePeca,
                CodigoIdentificador = p.Peca?.CodigoIdentificador,
                Quantidade = p.Quantidade,
                ValorUnitario = p.ValorUnitario,
                PrecoOriginal = p.Peca?.Preco,
                UnidadeMedida = p.Peca?.UnidadeMedida
            }).ToList() ?? new List<OrdemServicoPecaDto>(),

            Orcamentos = ordemServico.Orcamentos?.Select(o => new OrcamentoDto
            {
                Id = o.Id,
                DataOrcamento = o.DataOrcamento,
                IdStatus = o.IdStatus,
                StatusDescricao = o.Status?.Descricao,
                ValorServicos = ordemServico.OrdemServicoServicos?.Sum(s => s.ValorAplicado ?? 0) ?? 0,
                ValorPecas = ordemServico.OrdemServicoPecas?.Sum(p => (p.ValorUnitario ?? 0) * p.Quantidade) ?? 0,
                ValorTotal = (ordemServico.OrdemServicoServicos?.Sum(s => s.ValorAplicado ?? 0) ?? 0) +
                            (ordemServico.OrdemServicoPecas?.Sum(p => (p.ValorUnitario ?? 0) * p.Quantidade) ?? 0)
            }).ToList() ?? new List<OrcamentoDto>()
        };
    }

    private FasesTempoMedioDto CalcularFasesDetalhadas(List<OrdemServico> ordens)
    {
        var ordensComDatas = ordens.Where(o => o.DataFinalizacao.HasValue && o.DataEntrega.HasValue).ToList();

        if (!ordensComDatas.Any())
        {
            var tempoMedio = ordens.Where(o => o.DataEntrega.HasValue)
                .Average(o => (o.DataEntrega!.Value - o.DataSolicitacao).TotalHours);

            return new FasesTempoMedioDto
            {
                TempoMedioDiagnosticoHoras = Math.Round(tempoMedio * 0.3, 2),
                TempoMedioExecucaoHoras = Math.Round(tempoMedio * 0.6, 2),
                TempoMedioEntregaHoras = Math.Round(tempoMedio * 0.1, 2)
            };
        }

        var tempoMedioExecucaoHoras = ordensComDatas
            .Average(o => (o.DataFinalizacao!.Value - o.DataSolicitacao).TotalHours);

        var tempoMedioEntregaHoras = ordensComDatas
            .Average(o => (o.DataEntrega!.Value - o.DataFinalizacao!.Value).TotalHours);

        var tempoMedioDiagnosticoHoras = Math.Max(0,
            ordens.Where(o => o.DataEntrega.HasValue)
                .Average(o => (o.DataEntrega!.Value - o.DataSolicitacao).TotalHours)
            - tempoMedioExecucaoHoras - tempoMedioEntregaHoras);

        return new FasesTempoMedioDto
        {
            TempoMedioDiagnosticoHoras = Math.Round(tempoMedioDiagnosticoHoras, 2),
            TempoMedioExecucaoHoras = Math.Round(tempoMedioExecucaoHoras, 2),
            TempoMedioEntregaHoras = Math.Round(tempoMedioEntregaHoras, 2)
        };
    }

    private static ServicoTempoDetalheDto MapearServicoDetalheReal(OrdemServico ordem)
    {
        var dataEntrega = ordem.DataEntrega ?? ordem.DataSolicitacao.AddDays(5); 
        var tempo = dataEntrega - ordem.DataSolicitacao;

        return new ServicoTempoDetalheDto
        {
            IdOrdemServico = ordem.Id,
            ClienteNome = ordem.Veiculo?.Cliente?.Nome ?? "N/A",
            VeiculoPlaca = ordem.Veiculo?.Placa ?? "N/A",
            VeiculoMarcaModelo = ordem.Veiculo?.MarcaModelo ?? "N/A",
            DataRecebimento = ordem.DataSolicitacao,
            DataEntrega = dataEntrega,
            TotalDias = Math.Round(tempo.TotalDays, 2),
            TotalHoras = Math.Round(tempo.TotalHours, 2),
            ValorTotal = CalcularValorTotalOrdemServico(ordem)
        };
    }

    public async Task<ResumoTempoExecucaoDto> ObterResumoTempoExecucaoAsync()
    {
        var todasOrdens = await _ordemServicoRepository.GetAllWithDetailsAsync();
         var ordensResumo = todasOrdens
            .OrderByDescending(o => o.DataSolicitacao)
            .Select(MapearOrdemParaResumo)
            .ToList();

        var estatisticasGerais = CalcularEstatisticasGerais(todasOrdens);
        var tempoMedioPorCliente = CalcularTempoMedioPorCliente(todasOrdens);

        return new ResumoTempoExecucaoDto
        {
            OrdensServico = ordensResumo,
            EstatisticasGerais = estatisticasGerais,
            TempoMedioPorCliente = tempoMedioPorCliente
        };
    }

    private static OrdemServicoResumoDto MapearOrdemParaResumo(OrdemServico ordem)
    {
        return new OrdemServicoResumoDto
        {
            IdOrdemServico = ordem.Id,
            ClienteNome = ordem.Veiculo?.Cliente?.Nome ?? "N/A",
            VeiculoPlaca = ordem.Veiculo?.Placa ?? "N/A",
            VeiculoMarcaModelo = ordem.Veiculo?.MarcaModelo ?? "N/A",
            DataSolicitacao = ordem.DataSolicitacao,
            DataFinalizacao = ordem.DataFinalizacao,
            DataEntrega = ordem.DataEntrega,
            StatusDescricao = ordem.Status?.Descricao ?? "N/A",
            ValorTotal = CalcularValorTotalOrdemServico(ordem)
        };
    }

    private static EstatisticasGeraisDto CalcularEstatisticasGerais(IEnumerable<OrdemServico> ordens)
    {
        var ordensLista = ordens.ToList();
        var ordensFinalizadas = ordensLista.Where(o => o.DataFinalizacao.HasValue).ToList();
        var ordensEntregues = ordensLista.Where(o => o.DataEntrega.HasValue).ToList();

        var tempoMedioFinalizacaoHoras = ordensFinalizadas.Any()
            ? ordensFinalizadas.Average(o => (o.DataFinalizacao!.Value - o.DataSolicitacao).TotalHours)
            : 0;

        var tempoMedioEntregaHoras = ordensEntregues.Any()
            ? ordensEntregues.Average(o => (o.DataEntrega!.Value - o.DataSolicitacao).TotalHours)
            : 0;

        return new EstatisticasGeraisDto
        {
            TotalOrdensAnalisadas = ordensLista.Count,
            TotalOrdensFinalizadas = ordensFinalizadas.Count,
            TotalOrdensEntregues = ordensEntregues.Count,
            TempoMedioFinalizacaoHoras = Math.Round(tempoMedioFinalizacaoHoras, 2),
            TempoMedioEntregaHoras = Math.Round(tempoMedioEntregaHoras, 2)
        };
    }

    private static List<TempoMedioPorClienteDto> CalcularTempoMedioPorCliente(IEnumerable<OrdemServico> ordens)
    {
        var ordensEntregues = ordens
            .Where(o => o.DataEntrega.HasValue && o.Veiculo?.Cliente != null)
            .ToList();

        if (!ordensEntregues.Any())
            return new List<TempoMedioPorClienteDto>();

        return ordensEntregues
            .GroupBy(o => new
            {
                IdCliente = o.Veiculo!.Cliente!.Id,
                NomeCliente = o.Veiculo.Cliente.Nome
            })
            .Select(grupo => new TempoMedioPorClienteDto
            {
                IdCliente = grupo.Key.IdCliente,
                NomeCliente = grupo.Key.NomeCliente ?? "N/A",
                TotalOrdensEntregues = grupo.Count(),
                TempoMedioHoras = Math.Round(grupo.Average(o =>
                    (o.DataEntrega!.Value - o.DataSolicitacao).TotalHours), 2),
                ValorMedioOrdem = Math.Round(grupo.Average(o =>
                    CalcularValorTotalOrdemServico(o)), 2)
            })
            .OrderBy(c => c.TempoMedioHoras)
            .ToList();
    }
}