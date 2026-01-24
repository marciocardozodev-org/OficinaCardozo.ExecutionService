using Microsoft.AspNetCore.Mvc;
using OficinaCardozo.Application.DTOs;
using OficinaCardozo.Application.Services;
using OficinaCardozo.Application.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Threading;

namespace OficinaCardozo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdemServicoFullFlowController : ControllerBase
    {
        private readonly IOrdemServicoService _ordemServicoService;
        private readonly IClienteService _clienteService;
        private readonly IServicoService _servicoService;
        private readonly IVeiculoService _veiculoService;
        private readonly Random _random = new Random();

        public OrdemServicoFullFlowController(
            IOrdemServicoService ordemServicoService,
            IClienteService clienteService,
            IServicoService servicoService,
            IVeiculoService veiculoService)
        {
            _ordemServicoService = ordemServicoService;
            _clienteService = clienteService;
            _servicoService = servicoService;
            _veiculoService = veiculoService;
        }

        [HttpPost("executar-fluxo-completo")] // POST /api/OrdemServicoFullFlow/executar-fluxo-completo
        public async Task<IActionResult> ExecutarFluxoCompleto([FromQuery] int quantidade = 10)
        {
            var results = new List<object>();
            // Busca ou cria um serviço válido
            var servicos = await _servicoService.GetAllAsync();
            var servico = servicos.FirstOrDefault();
            if (servico == null)
            {
                var novoServico = new CreateServicoDto { NomeServico = "Serviço Teste", Preco = 100, TempoEstimadoExecucao = 1 };
                servico = await _servicoService.CreateAsync(novoServico);
            }
            for (int i = 0; i < quantidade; i++)
            {
                var cpf = $"888888880{i:00}";
                var placa = $"FULL{i:00}";
                // Busca ou cria cliente
                var cliente = (await _clienteService.ObterTodosClientesAsync()).FirstOrDefault(c => c.CpfCnpj == cpf);
                if (cliente == null)
                {
                    var novoCliente = new CreateClienteDto { Nome = $"Cliente Full {i}", CpfCnpj = cpf, EmailPrincipal = $"full{i}@teste.com", TelefonePrincipal = "11999990000" };
                    cliente = await _clienteService.CriarClienteAsync(novoCliente);
                }
                // Busca ou cria veículo
                var veiculo = await _veiculoService.GetByPlacaAsync(placa);
                if (veiculo == null)
                {
                    var novoVeiculo = new CreateVeiculoDto { Placa = placa, MarcaModelo = "Modelo Full", AnoFabricacao = 2021, Cor = "Preto", TipoCombustivel = "Flex", IdCliente = cliente.Id };
                    veiculo = await _veiculoService.CreateAsync(novoVeiculo);
                }
                var createDto = new CreateOrdemServicoDto
                {
                    ClienteCpfCnpj = cpf,
                    VeiculoPlaca = placa,
                    VeiculoMarcaModelo = veiculo.MarcaModelo,
                    VeiculoAnoFabricacao = veiculo.AnoFabricacao,
                    VeiculoCor = veiculo.Cor,
                    VeiculoTipoCombustivel = veiculo.TipoCombustivel,
                    ServicosIds = new List<int> { servico.Id },
                    Pecas = new List<CreateOrdemServicoPecaDto>()
                };
                try
                {
                    var ordem = await _ordemServicoService.CreateOrdemServicoComOrcamentoAsync(createDto);
                    await Task.Delay(_random.Next(500, 2000));
                    await _ordemServicoService.IniciarDiagnosticoAsync(ordem.Id);
                    await Task.Delay(_random.Next(500, 2000));
                    await _ordemServicoService.FinalizarDiagnosticoAsync(ordem.Id);
                    await Task.Delay(_random.Next(500, 2000));
                    await _ordemServicoService.IniciarExecucaoAsync(ordem.Id);
                    await Task.Delay(_random.Next(500, 2000));
                    await _ordemServicoService.FinalizarServicoAsync(ordem.Id);
                    await Task.Delay(_random.Next(500, 2000));
                    await _ordemServicoService.EntregarVeiculoAsync(ordem.Id);
                    results.Add(new { ordem.Id, Status = "OK" });
                }
                catch (Exception ex)
                {
                    results.Add(new { Ordem = i, Error = ex.Message });
                }
            }
            return Ok(results);
        }
    }
}
