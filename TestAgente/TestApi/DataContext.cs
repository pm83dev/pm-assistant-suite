using Microsoft.EntityFrameworkCore;
using TestApi.Models;

namespace TestApi.Services;

public class DataContext : DbContext
{
    public DbSet<Cliente> Clienti { get; set; }
    public DbSet<Progetto> Progetti { get; set; }
    public DbSet<OraLavorata> OreLavorate { get; set; }
    public DbSet<Nota> Note { get; set; }

    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configura Cliente
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Telefono).HasMaxLength(200);
            entity.Property(e => e.Indirizzo).HasMaxLength(500);
        });

        // Configura Progetto
        modelBuilder.Entity<Progetto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Descrizione).HasMaxLength(500);
            entity.HasOne(e => e.Cliente)
                  .WithMany(c => c.Progetti)
                  .HasForeignKey(e => e.ClienteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configura OraLavorata
        modelBuilder.Entity<OraLavorata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Data).IsRequired();
            entity.Property(e => e.Ore).IsRequired();
            entity.Property(e => e.Descrizione).HasMaxLength(500);
            entity.HasOne(e => e.Progetto)
                  .WithMany(p => p.OreLavorate)
                  .HasForeignKey(e => e.ProgettoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configura Nota
        modelBuilder.Entity<Nota>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DataCreazione).IsRequired();
            entity.Property(e => e.Contenuto).IsRequired();
            entity.Property(e => e.Titolo).HasMaxLength(500);
            entity.HasOne(e => e.Progetto)
                  .WithMany(p => p.Note)
                  .HasForeignKey(e => e.ProgettoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
