using Microsoft.EntityFrameworkCore;
using OFICINACARDOZO.OSSERVICE.Domain;

namespace OFICINACARDOZO.OSSERVICE.InfraDb
{
    public class OsDbContext : DbContext
    {
        public OsDbContext(DbContextOptions<OsDbContext> options) : base(options) { }

        public DbSet<OrdemDeServico> OrdensDeServico { get; set; }
        public DbSet<OrdemDeServicoHistorico> OrdensDeServicoHistorico { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<OrdemDeServico>().ToTable("OFICINA_ORDEM_SERVICO");
            modelBuilder.Entity<OrdemDeServicoHistorico>().ToTable("OFICINA_ORDEM_SERVICO_HISTORICO");
        }
    }
}
