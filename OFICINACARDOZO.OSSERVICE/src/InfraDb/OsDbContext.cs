using Microsoft.EntityFrameworkCore;
using OFICINACARDOZO.OSSERVICE.Domain;

namespace OFICINACARDOZO.OSSERVICE.InfraDb
{
    public class OsDbContext : DbContext
    {
        public OsDbContext(DbContextOptions<OsDbContext> options) : base(options) { }

        public DbSet<OrdemDeServico> OrdensDeServico { get; set; }
    }
}
