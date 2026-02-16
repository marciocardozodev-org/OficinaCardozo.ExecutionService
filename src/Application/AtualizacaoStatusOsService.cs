using OFICINACARDOZO.BILLINGSERVICE.Domain;

namespace OFICINACARDOZO.BILLINGSERVICE.Application
{
    public class AtualizacaoStatusOsService
    {
        private readonly List<AtualizacaoStatusOs> _atualizacoes = new();
        public AtualizacaoStatusOs AtualizarStatus(int ordemServicoId, string novoStatus)
        {
            var atualizacao = new AtualizacaoStatusOs
            {
                OrdemServicoId = ordemServicoId,
                NovoStatus = novoStatus,
                AtualizadoEm = DateTime.UtcNow
            };
            _atualizacoes.Add(atualizacao);
            return atualizacao;
        }
        public IEnumerable<AtualizacaoStatusOs> ListarPorOrdem(int ordemServicoId) => _atualizacoes.Where(a => a.OrdemServicoId == ordemServicoId);
    }
}