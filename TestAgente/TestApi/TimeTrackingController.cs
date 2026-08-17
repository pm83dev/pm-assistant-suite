using Microsoft.AspNetCore.Mvc;
using TestApi.Models;
using TestApi.Services;

namespace TestApi.Controllers;

[ApiController]
[Route("api/time-tracking")]
public class TimeTrackingController : ControllerBase
{
    private readonly IDataRepository _repository;
    private readonly ILogger<TimeTrackingController> _logger;

    public TimeTrackingController(IDataRepository repository, ILogger<TimeTrackingController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // ==================== CLIENTI ====================

    [HttpGet("clienti")]
    public async Task<ActionResult<IEnumerable<Cliente>>> GetClienti()
    {
        var clienti = await _repository.GetAllClientiAsync();
        return Ok(clienti);
    }

    [HttpGet("clienti/{id}")]
    public async Task<ActionResult<Cliente>> GetCliente(int id)
    {
        var cliente = await _repository.GetClienteByIdAsync(id);
        if (cliente == null) return NotFound();
        return Ok(cliente);
    }

    [HttpPost("clienti")]
    public async Task<ActionResult<Cliente>> CreateCliente(Cliente cliente)
    {
        var created = await _repository.AddClienteAsync(cliente);
        return CreatedAtAction(nameof(GetCliente), new { id = created.Id }, created);
    }

    [HttpPut("clienti/{id}")]
    public async Task<IActionResult> UpdateCliente(int id, Cliente cliente)
    {
        if (id != cliente.Id) return BadRequest();
        await _repository.UpdateClienteAsync(cliente);
        return NoContent();
    }

    [HttpDelete("clienti/{id}")]
    public async Task<IActionResult> DeleteCliente(int id)
    {
        await _repository.DeleteClienteAsync(id);
        return NoContent();
    }

    // ==================== PROGETTI ====================

    [HttpGet("progetti")]
    public async Task<ActionResult<IEnumerable<Progetto>>> GetProgetti()
    {
        var progetti = await _repository.GetAllProgettiAsync();
        return Ok(progetti);
    }

    [HttpGet("progetti/cliente/{clienteId}")]
    public async Task<ActionResult<IEnumerable<Progetto>>> GetProgettiByCliente(int clienteId)
    {
        var progetti = await _repository.GetProgettiByClienteAsync(clienteId);
        return Ok(progetti);
    }

    [HttpGet("progetti/{id}")]
    public async Task<ActionResult<Progetto>> GetProgetto(int id)
    {
        var progetto = await _repository.GetProgettoByIdAsync(id);
        if (progetto == null) return NotFound();
        return Ok(progetto);
    }

    [HttpPost("progetti")]
    public async Task<ActionResult<Progetto>> CreateProgetto(Progetto progetto)
    {
        var created = await _repository.AddProgettoAsync(progetto);
        return CreatedAtAction(nameof(GetProgetto), new { id = created.Id }, created);
    }

    [HttpPut("progetti/{id}")]
    public async Task<IActionResult> UpdateProgetto(int id, Progetto progetto)
    {
        if (id != progetto.Id) return BadRequest();
        await _repository.UpdateProgettoAsync(progetto);
        return NoContent();
    }

    [HttpDelete("progetti/{id}")]
    public async Task<IActionResult> DeleteProgetto(int id)
    {
        await _repository.DeleteProgettoAsync(id);
        return NoContent();
    }

    // ==================== ORE LAVORATE ====================

    [HttpGet("ore")]
    public async Task<ActionResult<IEnumerable<OraLavorata>>> GetOreLavorate()
    {
        var ore = await _repository.GetAllOreLavorateAsync();
        return Ok(ore);
    }

    [HttpGet("ore/progetto/{progettoId}")]
    public async Task<ActionResult<IEnumerable<OraLavorata>>> GetOreByProgetto(int progettoId)
    {
        var ore = await _repository.GetOreByProgettoAsync(progettoId);
        return Ok(ore);
    }

    [HttpGet("ore/range")]
    public async Task<ActionResult<IEnumerable<OraLavorata>>> GetOreByRange([FromQuery] DateTime da, [FromQuery] DateTime a)
    {
        var ore = await _repository.GetOreByDataRangeAsync(da, a);
        return Ok(ore);
    }

    [HttpGet("ore/progetto/{progettoId}/total")]
    public async Task<ActionResult<decimal>> GetTotalOreByProgetto(int progettoId)
    {
        var total = await _repository.GetTotalOreByProgettoAsync(progettoId);
        return Ok(new { ProgettoId = progettoId, TotalOre = total });
    }

    [HttpPost("ore")]
    public async Task<ActionResult<OraLavorata>> CreateOraLavorata(OraLavorata ora)
    {
        ora.Data = DateTime.Now.Date;
        var created = await _repository.AddOraLavorataAsync(ora);
        return CreatedAtAction(nameof(GetOreByProgetto), new { progettoId = created.ProgettoId }, created);
    }

    [HttpPut("ore/{id}")]
    public async Task<IActionResult> UpdateOraLavorata(int id, OraLavorata ora)
    {
        if (id != ora.Id) return BadRequest();
        await _repository.UpdateOraLavorataAsync(ora);
        return NoContent();
    }

    [HttpDelete("ore/{id}")]
    public async Task<IActionResult> DeleteOraLavorata(int id)
    {
        await _repository.DeleteOraLavorataAsync(id);
        return NoContent();
    }

    // ==================== NOTE ====================

    [HttpGet("note")]
    public async Task<ActionResult<IEnumerable<Nota>>> GetNote()
    {
        var note = await _repository.GetAllNoteAsync();
        return Ok(note);
    }

    [HttpGet("note/progetto/{progettoId}")]
    public async Task<ActionResult<IEnumerable<Nota>>> GetNoteByProgetto(int progettoId)
    {
        var note = await _repository.GetNoteByProgettoAsync(progettoId);
        return Ok(note);
    }

    [HttpPost("note")]
    public async Task<ActionResult<Nota>> CreateNota(Nota nota)
    {
        nota.DataCreazione = DateTime.Now;
        var created = await _repository.AddNotaAsync(nota);
        return CreatedAtAction(nameof(GetNoteByProgetto), new { progettoId = created.ProgettoId }, created);
    }

    [HttpPut("note/{id}")]
    public async Task<IActionResult> UpdateNota(int id, Nota nota)
    {
        if (id != nota.Id) return BadRequest();
        await _repository.UpdateNotaAsync(nota);
        return NoContent();
    }

    [HttpDelete("note/{id}")]
    public async Task<IActionResult> DeleteNota(int id)
    {
        await _repository.DeleteNotaAsync(id);
        return NoContent();
    }
}
