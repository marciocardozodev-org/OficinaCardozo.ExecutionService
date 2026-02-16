using Microsoft.AspNetCore.Mvc;
using OFICINACARDOZO.OSSERVICE.Domain;
using OFICINACARDOZO.OSSERVICE.Infrastructure;
using System.Threading.Tasks;

namespace OFICINACARDOZO.OSSERVICE.API
{
    [ApiController]
    [Route("api/[controller]")]
    /// <summary>
    /// Endpoints para gestão de Ordens de Serviço.
    /// </summary>
    [ApiExplorerSettings(GroupName = "v1")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class OrdemDeServicoController : ControllerBase
    {
        private readonly OrdemDeServicoEfRepository _repository;
        private readonly ILogger<OrdemDeServicoController> _logger;

        public OrdemDeServicoController(OrdemDeServicoEfRepository repository, ILogger<OrdemDeServicoController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <summary>
        /// Cria uma nova Ordem de Serviço.
        /// </summary>
        /// <param name="descricao">Descrição da OS</param>
        /// <remarks>
        /// Exemplo de request:
        ///
        ///     "Troca de óleo do veículo X"
        ///
        /// Exemplo de response:
        ///
        ///     {
        ///         "id": "guid",
        ///         "descricao": "Troca de óleo do veículo X",
        ///         "dataCriacao": "2026-02-15T12:00:00Z",
        ///         "status": "Aberta"
        ///     }
        /// </remarks>
        /// <returns>Ordem de Serviço criada</returns>
        /// <response code="201">Criado com sucesso</response>
        /// <response code="400">Descrição obrigatória</response>
        /// <response code="500">Erro interno</response>
        [HttpPost]
        [ProducesResponseType(typeof(OrdemDeServico), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<OrdemDeServico>> Criar([FromBody] OrdemDeServico ordem)
        {
            if (ordem == null)
            {
                _logger.LogWarning("Tentativa de criar OS com dados nulos.");
                return BadRequest(new { Erro = "Dados obrigatórios." });
            }
            await _repository.AddAsync(ordem);
            _logger.LogInformation("Ordem de Serviço criada com ID {Id}", ordem.Id);
            return CreatedAtAction(nameof(ObterPorId), new { id = ordem.Id }, ordem);
        }

        /// <summary>
        /// Altera o status de uma Ordem de Serviço.
        /// </summary>
        /// <param name="id">ID da OS</param>
        /// <param name="novoStatus">Novo status</param>
        /// <returns>204 se alterado, 404 se não encontrada</returns>
            /// <summary>
            /// Altera o status de uma Ordem de Serviço.
            /// </summary>
            /// <param name="id">ID da OS</param>
            /// <param name="novoStatus">Novo status (Aberta, EmAndamento, Finalizada, Cancelada)</param>
            /// <summary>
            /// Lista Ordens de Serviço por status.
            /// </summary>
            /// <param name="status">Status desejado (Aberta, EmAndamento, Finalizada, Cancelada)</param>
            /// <summary>
            /// Lista Ordens de Serviço por data de criação.
            /// </summary>
            /// <param name="data">Data no formato yyyy-MM-dd</param>
            /// <remarks>
            /// Exemplo de response:
            ///
            ///     [
            ///         {
            ///             "id": "guid",
            ///             "descricao": "Troca de óleo do veículo X",
            ///             "dataCriacao": "2026-02-15T12:00:00Z",
            ///             "status": "Aberta"
            ///         }
            ///     ]
            /// </remarks>
            /// <returns>Lista de OS</returns>
        /// Lista Ordens de Serviço por status.
        /// </summary>
        /// <param name="status">Status desejado</param>
        /// <returns>Lista de OS</returns>
        [HttpGet("status/{status}")]
        [ProducesResponseType(typeof(IEnumerable<OrdemDeServico>), 200)]
        public async Task<ActionResult<IEnumerable<OrdemDeServico>>> ListarPorStatus(int status)
        {
            var ordens = await _repository.GetByStatusAsync(status);
            return Ok(ordens);
        }

        /// <summary>
        /// Lista Ordens de Serviço por data de criação.
        /// </summary>
        /// <param name="data">Data no formato yyyy-MM-dd</param>
        /// <returns>Lista de OS</returns>
        [HttpGet("data/{data}")]
        [ProducesResponseType(typeof(IEnumerable<OrdemDeServico>), 200)]
        public async Task<ActionResult<IEnumerable<OrdemDeServico>>> ListarPorData(string data)
        {
            if (!DateTime.TryParse(data, out var dt))
                return BadRequest(new { Erro = "Data inválida. Use yyyy-MM-dd." });
            var ordens = await _repository.GetByDateAsync(dt);
            return Ok(ordens);
        }

        /// <summary>
        /// Lista todas as Ordens de Serviço.
        /// </summary>
        /// <remarks>
        /// Exemplo de response:
        ///
        ///     [
        ///         {
        ///             "id": "guid",
        ///             "descricao": "Troca de óleo do veículo X",
        ///             "dataCriacao": "2026-02-15T12:00:00Z",
        ///             "status": "Aberta"
        ///         }
        ///     ]
        /// </remarks>
        /// <returns>Lista de OS</returns>
        /// <response code="200">Lista retornada com sucesso</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OrdemDeServico>), 200)]
        public async Task<ActionResult<IEnumerable<OrdemDeServico>>> Listar()
        {
            var ordens = await _repository.GetAllAsync();
            return Ok(ordens);
        }

        /// <summary>
        /// Busca uma Ordem de Serviço pelo ID.
        /// </summary>
        /// <param name="id">ID da OS</param>
        /// <remarks>
        /// Exemplo de response:
        ///
        ///     {
        ///         "id": "guid",
        ///         "descricao": "Troca de óleo do veículo X",
        ///         "dataCriacao": "2026-02-15T12:00:00Z",
        ///         "status": "Aberta"
        ///     }
        /// </remarks>
        /// <returns>Ordem de Serviço encontrada</returns>
        /// <response code="200">Encontrada</response>
        /// <response code="404">Não encontrada</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OrdemDeServico), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<OrdemDeServico>> ObterPorId(int id)
        {
            var ordem = await _repository.GetByIdAsync(id);
            if (ordem == null) return NotFound();
            return Ok(ordem);
        }

        /// <summary>
        /// Altera o status de uma Ordem de Serviço.
        /// </summary>
        /// <param name="id">ID da OS</param>
        /// <param name="novoStatus">Novo status (Aberta, EmAndamento, Finalizada, Cancelada)</param>
        /// <remarks>
        /// Exemplo de request:
        ///
        ///     "Finalizada"
        ///
        /// Exemplo de response (204):
        ///     (sem conteúdo)
        /// </remarks>
        /// <returns>204 se alterado, 404 se não encontrada</returns>
        [HttpPatch("{id}/status")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> AlterarStatus(int id, [FromBody] int novoStatus)
        {
            var ok = await _repository.UpdateStatusAsync(id, novoStatus);
            if (!ok) return NotFound();
            _logger.LogInformation("Status da OS {Id} alterado para {Status}", id, novoStatus);
            return NoContent();
        }

        /// <summary>
        /// Consulta o histórico de status de uma Ordem de Serviço.
        /// </summary>
        /// <param name="id">ID da OS</param>
        /// <returns>Lista de status e datas</returns>
        [HttpGet("{id}/historico")]
        [ProducesResponseType(typeof(IEnumerable<OrdemDeServicoHistorico>), 200)]
        public async Task<ActionResult<IEnumerable<OrdemDeServicoHistorico>>> Historico(int id)
        {
            var historico = await _repository.GetHistoricoAsync(id);
            if (historico == null || !historico.Any()) return NotFound();
            return Ok(historico);
        }
    }
}
