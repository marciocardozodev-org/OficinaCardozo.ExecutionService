using Microsoft.EntityFrameworkCore;
using OFICINACARDOZO.EXECUTIONSERVICE.Domain;

namespace OFICINACARDOZO.EXECUTIONSERVICE
{
    public class ExecutionDbContext : DbContext
    {
        public ExecutionDbContext(DbContextOptions<ExecutionDbContext> options) : base(options) { }

        public DbSet<Orcamento> Orcamentos { get; set; }
        public DbSet<Pagamento> Pagamentos { get; set; }
        public DbSet<AtualizacaoStatusOs> AtualizacoesStatusOs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Mapeamento de tabelas removido: agora feito por data annotations nas models
        }
    }
}
