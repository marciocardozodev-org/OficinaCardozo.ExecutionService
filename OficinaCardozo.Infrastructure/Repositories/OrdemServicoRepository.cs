using Microsoft.EntityFrameworkCore;
using OficinaCardozo.Domain.Entities;
using OficinaCardozo.Domain.Interfaces;
using OficinaCardozo.Infrastructure.Data;

namespace OficinaCardozo.Infrastructure.Repositories;

public class OrdemServicoRepository : IOrdemServicoRepository
{
    private readonly OficinaDbContext _context;

    public OrdemServicoRepository(OficinaDbContext context)
    {
        _context = context;
    }

    public async Task<OrdemServico> CreateAsync(OrdemServico ordemServico)
    {
        _context.OrdensServico.Add(ordemServico);
        await _context.SaveChangesAsync();
        return ordemServico;
    }

    public async Task<OrdemServico?> GetByIdAsync(int id)
    {
        return await _context.OrdensServico
            .AsNoTracking()
            .FirstOrDefaultAsync(os => os.Id == id);
    }

    public async Task<OrdemServico?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.OrdensServico
            .Include(os => os.Veiculo)
                .ThenInclude(v => v.Cliente)
            .Include(os => os.Status)
            .Include(os => os.OrdemServicoServicos)
                .ThenInclude(oss => oss.Servico)
            .Include(os => os.OrdemServicoPecas)
                .ThenInclude(osp => osp.Peca)
            .Include(os => os.Orcamentos)
                .ThenInclude(o => o.Status)
            .AsNoTracking()
            .FirstOrDefaultAsync(os => os.Id == id);
    }

    public async Task<IEnumerable<OrdemServico>> GetAllAsync()
    {
        return await _context.OrdensServico
            .AsNoTracking()
            .OrderByDescending(os => os.DataSolicitacao)
            .ToListAsync();
    }

    public async Task<IEnumerable<OrdemServico>> GetAllWithDetailsAsync()
    {
        return await _context.OrdensServico
            .Include(os => os.Veiculo)
                .ThenInclude(v => v.Cliente)
            .Include(os => os.Status)
            .Include(os => os.OrdemServicoServicos)
                .ThenInclude(oss => oss.Servico)
            .Include(os => os.OrdemServicoPecas)
                .ThenInclude(osp => osp.Peca)
            .Include(os => os.Orcamentos)
                .ThenInclude(o => o.Status)
            .AsNoTracking()
            .OrderByDescending(os => os.DataSolicitacao)
            .ToListAsync();
    }

    public async Task<OrdemServico> UpdateAsync(OrdemServico ordemServico)
    {
        var existingEntry = _context.ChangeTracker.Entries<OrdemServico>()
            .FirstOrDefault(e => e.Entity.Id == ordemServico.Id);

        if (existingEntry != null)
        {
            existingEntry.State = EntityState.Detached;
        }

        _context.OrdensServico.Update(ordemServico);
        var result = await _context.SaveChangesAsync();
        Console.WriteLine($"[DEBUG] UpdateAsync OrdemServico ID {ordemServico.Id} - Status atualizado para {ordemServico.IdStatus} - SaveChangesAsync result: {result}");
        return ordemServico;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var ordemServico = await _context.OrdensServico.FindAsync(id);
        if (ordemServico == null)
            return false;

        _context.OrdensServico.Remove(ordemServico);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> DeleteAllAsync()
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var orcamentos = await _context.Orcamentos.ToListAsync();
            _context.Orcamentos.RemoveRange(orcamentos);

            var ordemServicoServicos = await _context.OrdensServicoServicos.ToListAsync();
            _context.OrdensServicoServicos.RemoveRange(ordemServicoServicos);

            var ordemServicoPecas = await _context.OrdensServicoPecas.ToListAsync();
            _context.OrdensServicoPecas.RemoveRange(ordemServicoPecas);

            var ordensServico = await _context.OrdensServico.ToListAsync();
            var count = ordensServico.Count;
            _context.OrdensServico.RemoveRange(ordensServico);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return count;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<OrdemServicoServico> AddOrdemServicoServicoAsync(OrdemServicoServico ordemServicoServico)
    {
        _context.OrdensServicoServicos.Add(ordemServicoServico);
        await _context.SaveChangesAsync();
        return ordemServicoServico;
    }

    public async Task<OrdemServicoPeca> AddOrdemServicoPecaAsync(OrdemServicoPeca ordemServicoPeca)
    {
        _context.OrdensServicoPecas.Add(ordemServicoPeca);
        await _context.SaveChangesAsync();
        return ordemServicoPeca;
    }
}