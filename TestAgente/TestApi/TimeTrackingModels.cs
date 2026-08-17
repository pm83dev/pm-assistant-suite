using System.ComponentModel.DataAnnotations;

namespace TestApi.Models;

public class Cliente
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? Telefono { get; set; }

    [MaxLength(500)]
    public string? Indirizzo { get; set; }

    public ICollection<Progetto>? Progetti { get; set; }
}

public class Progetto
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descrizione { get; set; }

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public ICollection<OraLavorata>? OreLavorate { get; set; }
    public ICollection<Nota>? Note { get; set; }
}

public class OraLavorata
{
    public int Id { get; set; }

    [Required]
    public DateTime Data { get; set; }

    [Required]
    [Range(0.25, 24)]
    public decimal Ore { get; set; }

    [MaxLength(500)]
    public string? Descrizione { get; set; }

    public int ProgettoId { get; set; }
    public Progetto? Progetto { get; set; }
}

public class Nota
{
    public int Id { get; set; }

    [Required]
    public DateTime DataCreazione { get; set; }

    [Required]
    public string Contenuto { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Titolo { get; set; }

    public int ProgettoId { get; set; }
    public Progetto? Progetto { get; set; }
}
