using Microsoft.EntityFrameworkCore;
using OFICINACARDOZO.BILLINGSERVICE.Domain;

namespace OFICINACARDOZO.BILLINGSERVICE
{
    public class BillingDbContext : DbContext
    {
        public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }

        public DbSet<Orcamento> Orcamentos { get; set; }
        public DbSet<Pagamento> Pagamentos { get; set; }
        public DbSet<AtualizacaoStatusOs> AtualizacoesStatusOs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Orcamento>().ToTable("orcamento");
            modelBuilder.Entity<Pagamento>().ToTable("pagamento");
            modelBuilder.Entity<AtualizacaoStatusOs>().ToTable("atualizacao_status_os");
        }
    }
}
