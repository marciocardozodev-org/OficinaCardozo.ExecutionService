using OFICINACARDOZO.EXECUTIONSERVICE.Domain;

namespace OFICINACARDOZO.EXECUTIONSERVICE.Application
{
    public class ExecucaoOsService
    {
        private readonly ExecutionDbContext _context;
        public ExecucaoOsService(ExecutionDbContext context)
        {
            _context = context;
        }

        public ExecucaoOs CriarExecucao(int ordemServicoId)
        {
            var execucao = new ExecucaoOs
            {
                OrdemServicoId = ordemServicoId,
                StatusAtual = "Fila",
                InicioExecucao = null,
                FimExecucao = null,
                Diagnostico = null,
                Reparo = null,
                Finalizado = false
            };
            _context.ExecucoesOs.Add(execucao);
            _context.SaveChanges();
            return execucao;
        }

        public ExecucaoOs? ObterExecucao(int ordemServicoId)
        {
            return _context.ExecucoesOs.FirstOrDefault(e => e.OrdemServicoId == ordemServicoId);
        }

        public void AtualizarStatus(int ordemServicoId, string novoStatus)
        {
            var execucao = _context.ExecucoesOs.FirstOrDefault(e => e.OrdemServicoId == ordemServicoId);
            if (execucao != null)
            {
                execucao.StatusAtual = novoStatus;
                if (novoStatus == "Em Diagnóstico")
                    execucao.InicioExecucao = DateTime.UtcNow;
                if (novoStatus == "Finalizado")
                {
                    execucao.FimExecucao = DateTime.UtcNow;
                    execucao.Finalizado = true;
                }
                _context.SaveChanges();
            }
        }

        public void AtualizarDiagnostico(int ordemServicoId, string diagnostico)
        {
            var execucao = _context.ExecucoesOs.FirstOrDefault(e => e.OrdemServicoId == ordemServicoId);
            if (execucao != null)
            {
                execucao.Diagnostico = diagnostico;
                _context.SaveChanges();
            }
        }

        public void AtualizarReparo(int ordemServicoId, string reparo)
        {
            var execucao = _context.ExecucoesOs.FirstOrDefault(e => e.OrdemServicoId == ordemServicoId);
            if (execucao != null)
            {
                execucao.Reparo = reparo;
                _context.SaveChanges();
            }
        }
    }
}
