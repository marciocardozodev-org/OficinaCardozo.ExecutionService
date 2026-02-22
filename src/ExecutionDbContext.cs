using Microsoft.EntityFrameworkCore;
using OFICINACARDOZO.EXECUTIONSERVICE.Domain;
using OficinaCardozo.ExecutionService.Domain;
using OficinaCardozo.ExecutionService.Inbox;
using OficinaCardozo.ExecutionService.Outbox;

namespace OFICINACARDOZO.EXECUTIONSERVICE
{
    public class ExecutionDbContext : DbContext
    {
        public ExecutionDbContext(DbContextOptions<ExecutionDbContext> options) : base(options) { }

        // Removidos DbSets legados de Orcamento e Pagamento
        public DbSet<ExecucaoOs> ExecucoesOs { get; set; }
        public DbSet<AtualizacaoStatusOs> AtualizacoesStatusOs { get; set; }
        
        // Novos DbSets para ExecutionService
        public DbSet<ExecutionJob> ExecutionJobs { get; set; }
        public DbSet<InboxEvent> InboxEvents { get; set; }
        public DbSet<OutboxEvent> OutboxEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração de ExecutionJob
            modelBuilder.Entity<ExecutionJob>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OsId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CorrelationId).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.LastError).HasMaxLength(500).IsRequired(false);
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.Attempt).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired(false);
                entity.Property(e => e.FinishedAt).IsRequired(false);
                entity.HasIndex(e => e.OsId).IsUnique();
            });

            // Configuração de InboxEvent
            modelBuilder.Entity<InboxEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.EventId).IsUnique();
            });

            // Configuração de OutboxEvent
            modelBuilder.Entity<OutboxEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Payload).IsRequired();
                entity.HasIndex(e => new { e.Published, e.CreatedAt });
            });
        }
    }
}
