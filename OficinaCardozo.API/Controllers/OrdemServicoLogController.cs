using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Threading.Tasks;

namespace OficinaCardozo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdemServicoLogController : ControllerBase
    {
        [HttpPost("criar-e-logar")] // Simula criação de OS e logs correlacionados
        public async Task<IActionResult> CriarOrdemServicoComLogs([FromBody] OrdemServicoSimuladaDto dto)
        {
            Log.Information("Iniciando criação de ordem de serviço para cliente {ClienteId}", dto.ClienteId);
            try
            {
                // Simula validação
                if (string.IsNullOrWhiteSpace(dto.Descricao))
                {
                    Log.Warning("Descrição da OS não informada para cliente {ClienteId}", dto.ClienteId);
                    return BadRequest("Descrição obrigatória.");
                }

                // Simula processamento
                await Task.Delay(200); // Simula acesso a banco
                var ordemId = Guid.NewGuid();
                Log.Information("Ordem de serviço criada com sucesso: {OrdemId} para cliente {ClienteId}", ordemId, dto.ClienteId);

                // Simula etapa de notificação
                try
                {
                    await Task.Delay(100); // Simula envio de e-mail
                    Log.Information("Notificação enviada para cliente {ClienteId} referente à OS {OrdemId}", dto.ClienteId, ordemId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Erro ao notificar cliente {ClienteId} sobre a OS {OrdemId}", dto.ClienteId, ordemId);
                }

                return Ok(new { ordemId });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro inesperado ao criar ordem de serviço para cliente {ClienteId}", dto.ClienteId);
                return StatusCode(500, "Erro interno ao criar ordem de serviço.");
            }
        }
    }

    public class OrdemServicoSimuladaDto
    {
        public Guid ClienteId { get; set; }
        public string Descricao { get; set; } = string.Empty;
    }
}
