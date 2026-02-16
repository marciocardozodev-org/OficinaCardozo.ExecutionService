using OFICINACARDOZO.BILLINGSERVICE.Domain;

namespace OFICINACARDOZO.BILLINGSERVICE.Application
{
    public class PagamentoService
    {
        private readonly List<Pagamento> _pagamentos = new();
        public Pagamento RegistrarPagamento(int ordemServicoId, decimal valor, string metodo)
        {
            var pagamento = new Pagamento
            {
                Id = _pagamentos.Count + 1,
                OrdemServicoId = ordemServicoId,
                Valor = valor,
                Metodo = metodo,
                Status = StatusPagamento.Confirmado,
                CriadoEm = DateTime.UtcNow
            };
            _pagamentos.Add(pagamento);
            return pagamento;
        }
        public Pagamento? ObterPagamento(int pagamentoId) => _pagamentos.FirstOrDefault(p => p.Id == pagamentoId);
    }
}