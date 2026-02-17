using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OFICINACARDOZO.EXECUTIONSERVICE.Application;
using OFICINACARDOZO.EXECUTIONSERVICE.Domain;

namespace OFICINACARDOZO.EXECUTIONSERVICE.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExecutionController : ControllerBase
    {
        private readonly ExecucaoOsService _execucaoOsService;
        private readonly AtualizacaoStatusOsService _statusOsService;

        public ExecutionController(ExecucaoOsService execucaoOsService, AtualizacaoStatusOsService statusOsService)
        {
            _execucaoOsService = execucaoOsService;
            _statusOsService = statusOsService;
        }

        [HttpPost("criar")]
        public IActionResult CriarExecucao([FromBody] CriarExecucaoDto dto)
        {
            var execucao = _execucaoOsService.CriarExecucao(dto.OrdemServicoId);
            return Ok(execucao);
        }

        [HttpPut("status")]
        public IActionResult AtualizarStatus([FromBody] AtualizarStatusDto dto)
        {
            _execucaoOsService.AtualizarStatus(dto.OrdemServicoId, dto.NovoStatus);
            _statusOsService.AtualizarStatus(dto.OrdemServicoId, dto.NovoStatus);
            return Ok();
        }

        [HttpPut("diagnostico")]
        public IActionResult AtualizarDiagnostico([FromBody] AtualizarDiagnosticoDto dto)
        {
            _execucaoOsService.AtualizarDiagnostico(dto.OrdemServicoId, dto.Diagnostico);
            return Ok();
        }

        [HttpPut("reparo")]
        public IActionResult AtualizarReparo([FromBody] AtualizarReparoDto dto)
        {
            _execucaoOsService.AtualizarReparo(dto.OrdemServicoId, dto.Reparo);
            return Ok();
        }

        [HttpGet("{ordemServicoId}")]
        public IActionResult ObterExecucao(int ordemServicoId)
        {
            var execucao = _execucaoOsService.ObterExecucao(ordemServicoId);
            if (execucao == null) return NotFound();
            return Ok(execucao);
        }
    }

    public class CriarExecucaoDto
    {
        public int OrdemServicoId { get; set; }
    }
    public class AtualizarStatusDto
    {
        public int OrdemServicoId { get; set; }
        public string NovoStatus { get; set; } = string.Empty;
    }
    public class AtualizarDiagnosticoDto
    {
        public int OrdemServicoId { get; set; }
        public string Diagnostico { get; set; } = string.Empty;
    }
    public class AtualizarReparoDto
    {
        public int OrdemServicoId { get; set; }
        public string Reparo { get; set; } = string.Empty;
    }
}