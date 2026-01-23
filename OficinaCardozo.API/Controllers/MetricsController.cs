using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OficinaCardozo.Application.Services;
using System;

namespace OficinaCardozo.API.Controllers
{
    [ApiController]
    [Route("api/metrics")]
    public class MetricsController : ControllerBase
    {
        private readonly ILogger<MetricsController> _logger;
        private readonly IOrdemServicoService _ordemServicoService;

        public MetricsController(IOrdemServicoService ordemServicoService, ILogger<MetricsController> logger)
        {
            _ordemServicoService = ordemServicoService;
            _logger = logger;
        }

        [HttpPost("test-fail-real")]
        [AllowAnonymous]
        public async Task<IActionResult> TestFailReal()
        {
            _logger.LogInformation("TestFailReal endpoint chamado");
            // Dados mockados para garantir falha (cliente inexistente)
            var dto = new OficinaCardozo.Application.DTOs.CreateOrdemServicoDto
            {
                ClienteCpfCnpj = "00000000000", // CPF/CNPJ que não existe
                VeiculoPlaca = "ZZZ9999",
                VeiculoMarcaModelo = "Teste",
                VeiculoAnoFabricacao = 2020,
                ServicosIds = new List<int> { 99999 }, // ID de serviço inexistente
                Pecas = new List<OficinaCardozo.Application.DTOs.CreateOrdemServicoPecaDto> {
                    new OficinaCardozo.Application.DTOs.CreateOrdemServicoPecaDto { IdPeca = 99999, Quantidade = 1 }
                }
            };
            try
            {
                await _ordemServicoService.CreateOrdemServicoComOrcamentoAsync(dto);
                _logger.LogWarning("Teste não gerou falha como esperado");
                return Ok(new { message = "Teste não gerou falha como esperado" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha simulada com sucesso");
                return StatusCode(500, new { message = "Falha simulada com sucesso", details = ex.Message });
            }
        }

        [HttpPost("test-batch")]
        public IActionResult SendBatchMetrics([FromQuery] int count = 100)
        {
            for (int i = 0; i < count; i++)
            {
                StatsdClient.Metrics.Counter("echo_teste.metric", 1);
                StatsdClient.Metrics.Counter("nova_metrica.teste", 1);
            }
            return Ok(new { sent = count, timestamp = DateTime.UtcNow });
        }

        [HttpPost("test-fail")]
        public IActionResult SendFailMetric([FromQuery] int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                StatsdClient.Metrics.Counter("ordem_servico.fail", 1);
            }
            return Ok(new { sent = count, timestamp = DateTime.UtcNow });
        }

        [HttpPost("simulate-fail")]
        public IActionResult SimulateBusinessFail([FromQuery] int count = 1)
        {
            int failures = 0;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    // Simula uma lógica de negócio que pode falhar
                    throw new InvalidOperationException("Simulação de falha de ordem de serviço");
                }
                catch (Exception ex)
                {
                    StatsdClient.Metrics.Counter("ordem_servico.fail", 1);
                    failures++;
                }
            }
            return StatusCode(500, new { failed = failures, timestamp = DateTime.UtcNow });
        }
    }
}
