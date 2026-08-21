using Microsoft.EntityFrameworkCore;
using OreTracking.Api.Models;

namespace OreTracking.Api.Services;

public class DataService : IDataRepository
{
    private readonly DataContext _context;

    public DataService(DataContext context)
    {
        _context = context;
    }

    // Cliente
    public IQueryable<Cliente> GetAllClienti() => _context.Clienti.AsQueryable();
    public async Task<IEnumerable<Cliente>> GetAllClientiAsync() => await _context.Clienti.ToListAsync();
    public Cliente? GetClienteById(int id) => _context.Clienti.FirstOrDefault(c => c.Id == id);
    public async Task<Cliente?> GetClienteByIdAsync(int id) => await _context.Clienti.FindAsync(id);
    public Cliente AddCliente(Cliente cliente) { _context.Clienti.Add(cliente); return cliente; }
    public async Task<Cliente> AddClienteAsync(Cliente cliente) { _context.Clienti.Add(cliente); await SaveChangesAsync(); return cliente; }
    public void UpdateCliente(Cliente cliente) => _context.Clienti.Update(cliente);
    public async Task<Cliente> UpdateClienteAsync(Cliente cliente) { _context.Clienti.Update(cliente); await SaveChangesAsync(); return cliente; }
    public void DeleteCliente(int id) { var c = _context.Clienti.Find(id); if (c != null) _context.Clienti.Remove(c); }
    public async Task DeleteClienteAsync(int id) { var c = await _context.Clienti.FindAsync(id); if (c != null) { _context.Clienti.Remove(c); await SaveChangesAsync(); } }

    // Progetto
    public IQueryable<Progetto> GetAllProgetti() => _context.Progetti.AsQueryable();
    public async Task<IEnumerable<Progetto>> GetAllProgettiAsync() => await _context.Progetti.ToListAsync();
    public Progetto? GetProgettoById(int id) => _context.Progetti.FirstOrDefault(p => p.Id == id);
    public async Task<Progetto?> GetProgettoByIdAsync(int id) => await _context.Progetti.FindAsync(id);
    public IEnumerable<Progetto> GetProgettiByCliente(int clienteId) => _context.Progetti.Where(p => p.ClienteId == clienteId);
    public async Task<IEnumerable<Progetto>> GetProgettiByClienteAsync(int clienteId) => await _context.Progetti.Where(p => p.ClienteId == clienteId).ToListAsync();
    public Progetto AddProgetto(Progetto progetto) { _context.Progetti.Add(progetto); return progetto; }
    public async Task<Progetto> AddProgettoAsync(Progetto progetto) { _context.Progetti.Add(progetto); await SaveChangesAsync(); return progetto; }
    public void UpdateProgetto(Progetto progetto) => _context.Progetti.Update(progetto);
    public async Task<Progetto> UpdateProgettoAsync(Progetto progetto) { _context.Progetti.Update(progetto); await SaveChangesAsync(); return progetto; }
    public void DeleteProgetto(int id) { var p = _context.Progetti.Find(id); if (p != null) _context.Progetti.Remove(p); }
    public async Task DeleteProgettoAsync(int id) { var p = await _context.Progetti.FindAsync(id); if (p != null) { _context.Progetti.Remove(p); await SaveChangesAsync(); } }

    // OraLavorata
    public IQueryable<OraLavorata> GetAllOreLavorate() => _context.OreLavorate.AsQueryable();
    public async Task<IEnumerable<OraLavorata>> GetAllOreLavorateAsync() => await _context.OreLavorate.ToListAsync();
    public IEnumerable<OraLavorata> GetOreByProgetto(int progettoId) => _context.OreLavorate.Where(o => o.ProgettoId == progettoId);
    public async Task<IEnumerable<OraLavorata>> GetOreByProgettoAsync(int progettoId) => await _context.OreLavorate.Where(o => o.ProgettoId == progettoId).ToListAsync();
    public IEnumerable<OraLavorata> GetOreByDataRange(DateTime da, DateTime a) => _context.OreLavorate.Where(o => o.Data >= da && o.Data <= a);
    public async Task<IEnumerable<OraLavorata>> GetOreByDataRangeAsync(DateTime da, DateTime a) => await _context.OreLavorate.Where(o => o.Data >= da && o.Data <= a).ToListAsync();
    public decimal GetTotalOreByProgetto(int progettoId) => _context.OreLavorate.Where(o => o.ProgettoId == progettoId).ToList().Sum(o => (decimal)o.Ore);
    public async Task<decimal> GetTotalOreByProgettoAsync(int progettoId) => (await _context.OreLavorate.Where(o => o.ProgettoId == progettoId).ToListAsync()).Sum(o => (decimal)o.Ore);
    public OraLavorata AddOraLavorata(OraLavorata ora) { _context.OreLavorate.Add(ora); return ora; }
    public async Task<OraLavorata> AddOraLavorataAsync(OraLavorata ora) { _context.OreLavorate.Add(ora); await SaveChangesAsync(); return ora; }
    public void UpdateOraLavorata(OraLavorata ora) => _context.OreLavorate.Update(ora);
    public async Task<OraLavorata> UpdateOraLavorataAsync(OraLavorata ora) { _context.OreLavorate.Update(ora); await SaveChangesAsync(); return ora; }
    public void DeleteOraLavorata(int id) { var o = _context.OreLavorate.Find(id); if (o != null) _context.OreLavorate.Remove(o); }
    public async Task DeleteOraLavorataAsync(int id) { var o = await _context.OreLavorate.FindAsync(id); if (o != null) { _context.OreLavorate.Remove(o); await SaveChangesAsync(); } }

    // Nota
    public IQueryable<Nota> GetAllNote() => _context.Note.AsQueryable();
    public async Task<IEnumerable<Nota>> GetAllNoteAsync() => await _context.Note.ToListAsync();
    public IEnumerable<Nota> GetNoteByProgetto(int progettoId) => _context.Note.Where(n => n.ProgettoId == progettoId);
    public async Task<IEnumerable<Nota>> GetNoteByProgettoAsync(int progettoId) => await _context.Note.Where(n => n.ProgettoId == progettoId).ToListAsync();
    public Nota AddNota(Nota nota) { _context.Note.Add(nota); return nota; }
    public async Task<Nota> AddNotaAsync(Nota nota) { _context.Note.Add(nota); await SaveChangesAsync(); return nota; }
    public void UpdateNota(Nota nota) => _context.Note.Update(nota);
    public async Task<Nota> UpdateNotaAsync(Nota nota) { _context.Note.Update(nota); await SaveChangesAsync(); return nota; }
    public void DeleteNota(int id) { var n = _context.Note.Find(id); if (n != null) _context.Note.Remove(n); }
    public async Task DeleteNotaAsync(int id) { var n = await _context.Note.FindAsync(id); if (n != null) { _context.Note.Remove(n); await SaveChangesAsync(); } }

    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();

    private bool disposed = false;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing) _context.Dispose();
            disposed = true;
        }
    }
    public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
}
