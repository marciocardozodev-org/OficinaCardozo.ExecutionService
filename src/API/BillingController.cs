using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OFICINACARDOZO.EXECUTIONSERVICE.Application;

namespace OFICINACARDOZO.EXECUTIONSERVICE.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class ExecutionController : ControllerBase
    {
        private readonly OrcamentoService _orcamentoService;
        private readonly PagamentoService _pagamentoService;
        private readonly AtualizacaoStatusOsService _statusOsService;

        public ExecutionController(OrcamentoService orcamentoService, PagamentoService pagamentoService, AtualizacaoStatusOsService statusOsService)
        {
            _orcamentoService = orcamentoService;
            _pagamentoService = pagamentoService;
            _statusOsService = statusOsService;
        }

        [HttpPost("orcamento")]
        public IActionResult GerarOrcamento([FromBody] OrcamentoRequestDto dto)
        {
            var orcamento = _orcamentoService.GerarEEnviarOrcamento(dto.OrdemServicoId, dto.Valor, dto.EmailCliente);
            return Ok(orcamento);
        }

        [HttpPost("pagamento")]
        public IActionResult RegistrarPagamento([FromBody] PagamentoRequestDto dto)
        {
            var pagamento = _pagamentoService.RegistrarPagamento(dto.OrdemServicoId, dto.Valor, dto.Metodo);
            return Ok(pagamento);
        }

        [HttpGet("pagamento/{id}")]
        public IActionResult ObterPagamento(int id)
        {
            var pagamento = _pagamentoService.ObterPagamento(id);
            if (pagamento == null) return NotFound();
            return Ok(pagamento);
        }

        [HttpPut("status-os")]
        public IActionResult AtualizarStatusOs([FromBody] AtualizacaoStatusOsDto dto)
        {
            var atualizacao = _statusOsService.AtualizarStatus(dto.OrdemServicoId, dto.NovoStatus);
            return Ok(atualizacao);
        }
    }

    public class OrcamentoRequestDto
    {
        public int OrdemServicoId { get; set; }
        public decimal Valor { get; set; }
        public string EmailCliente { get; set; } = string.Empty;
    }
    public class PagamentoRequestDto
    {
        public int OrdemServicoId { get; set; }
        public decimal Valor { get; set; }
        public string Metodo { get; set; } = string.Empty;
    }
    public class AtualizacaoStatusOsDto
    {
        public int OrdemServicoId { get; set; }
        public string NovoStatus { get; set; } = string.Empty;
    }
}