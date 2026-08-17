using OreTracking.Api.Models;

namespace OreTracking.Api.Services;

public interface IDataRepository : IDisposable
{
    // Cliente
    IQueryable<Cliente> GetAllClienti();
    Task<IEnumerable<Cliente>> GetAllClientiAsync();
    Cliente? GetClienteById(int id);
    Task<Cliente?> GetClienteByIdAsync(int id);
    Cliente AddCliente(Cliente cliente);
    Task<Cliente> AddClienteAsync(Cliente cliente);
    void UpdateCliente(Cliente cliente);
    Task<Cliente> UpdateClienteAsync(Cliente cliente);
    void DeleteCliente(int id);
    Task DeleteClienteAsync(int id);

    // Progetto
    IQueryable<Progetto> GetAllProgetti();
    Task<IEnumerable<Progetto>> GetAllProgettiAsync();
    Progetto? GetProgettoById(int id);
    Task<Progetto?> GetProgettoByIdAsync(int id);
    IEnumerable<Progetto> GetProgettiByCliente(int clienteId);
    Task<IEnumerable<Progetto>> GetProgettiByClienteAsync(int clienteId);
    Progetto AddProgetto(Progetto progetto);
    Task<Progetto> AddProgettoAsync(Progetto progetto);
    void UpdateProgetto(Progetto progetto);
    Task<Progetto> UpdateProgettoAsync(Progetto progetto);
    void DeleteProgetto(int id);
    Task DeleteProgettoAsync(int id);

    // OraLavorata
    IQueryable<OraLavorata> GetAllOreLavorate();
    Task<IEnumerable<OraLavorata>> GetAllOreLavorateAsync();
    IEnumerable<OraLavorata> GetOreByProgetto(int progettoId);
    Task<IEnumerable<OraLavorata>> GetOreByProgettoAsync(int progettoId);
    IEnumerable<OraLavorata> GetOreByDataRange(DateTime da, DateTime a);
    Task<IEnumerable<OraLavorata>> GetOreByDataRangeAsync(DateTime da, DateTime a);
    decimal GetTotalOreByProgetto(int progettoId);
    Task<decimal> GetTotalOreByProgettoAsync(int progettoId);
    OraLavorata AddOraLavorata(OraLavorata ora);
    Task<OraLavorata> AddOraLavorataAsync(OraLavorata ora);
    void UpdateOraLavorata(OraLavorata ora);
    Task<OraLavorata> UpdateOraLavorataAsync(OraLavorata ora);
    void DeleteOraLavorata(int id);
    Task DeleteOraLavorataAsync(int id);

    // Nota
    IQueryable<Nota> GetAllNote();
    Task<IEnumerable<Nota>> GetAllNoteAsync();
    IEnumerable<Nota> GetNoteByProgetto(int progettoId);
    Task<IEnumerable<Nota>> GetNoteByProgettoAsync(int progettoId);
    Nota AddNota(Nota nota);
    Task<Nota> AddNotaAsync(Nota nota);
    void UpdateNota(Nota nota);
    Task<Nota> UpdateNotaAsync(Nota nota);
    void DeleteNota(int id);
    Task DeleteNotaAsync(int id);
}
