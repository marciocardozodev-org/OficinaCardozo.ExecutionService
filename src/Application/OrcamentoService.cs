using OFICINACARDOZO.EXECUTIONSERVICE.Domain;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Application
{
    public class OrcamentoService
    {
        private readonly List<Orcamento> _orcamentos = new();
        public Orcamento GerarEEnviarOrcamento(int ordemServicoId, decimal valor, string emailCliente)
        {
            var orcamento = new Orcamento
            {
                Id = _orcamentos.Count + 1,
                OrdemServicoId = ordemServicoId,
                Valor = valor,
                EmailCliente = emailCliente,
                Status = StatusOrcamento.Enviado,
                CriadoEm = DateTime.UtcNow
            };
            _orcamentos.Add(orcamento);
            // Simular envio de orçamento por e-mail
            return orcamento;
        }
        public IEnumerable<Orcamento> ListarPorOrdem(int ordemServicoId) => _orcamentos.Where(o => o.OrdemServicoId == ordemServicoId);
    }
}