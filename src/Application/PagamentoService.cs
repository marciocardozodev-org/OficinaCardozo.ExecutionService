using OFICINACARDOZO.EXECUTIONSERVICE.Domain;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Application
{
    public class PagamentoService
    {
        private readonly ExecutionDbContext _context;
        public PagamentoService(ExecutionDbContext context)
        {
            _context = context;
        }

        public Pagamento RegistrarPagamento(int ordemServicoId, decimal valor, string metodo)
        {
            var pagamento = new Pagamento
            {
                OrdemServicoId = ordemServicoId,
                Valor = valor,
                Metodo = metodo,
                Status = StatusPagamento.Confirmado,
                CriadoEm = DateTime.UtcNow
            };
            _context.Pagamentos.Add(pagamento);
            _context.SaveChanges();
            return pagamento;
        }

        public Pagamento? ObterPagamento(int pagamentoId)
        {
            return _context.Pagamentos.FirstOrDefault(p => p.Id == pagamentoId);
        }
    }
}