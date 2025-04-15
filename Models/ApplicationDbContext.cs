using GestioneClienti.Entities;
using Microsoft.EntityFrameworkCore;
using WebAppEF.Entities;

namespace WebAppEF.Models
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {

        // Definizione delle DbSet per le entità
        public DbSet<Cliente> Clienti { get; set; }
        public DbSet<Prodotto> Prodotti { get; set; }
        public DbSet<Ordine> Ordini { get; set; }
        public DbSet<DettagliOrdine> DettagliOrdini { get; set; }
        public DbSet<Utente> Utenti { get; set; }

        public DbSet<CarrelloItem> CarrelloItems { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurazione delle colonne per i tipi DECIMAL
            modelBuilder.Entity<Prodotto>()
                .Property(p => p.Prezzo)
                .HasColumnType("DECIMAL(10,2)");

            modelBuilder.Entity<Ordine>()
                .Property(o => o.TotaleOrdine)
                .HasColumnType("DECIMAL(10,2)");

            modelBuilder.Entity<DettagliOrdine>()
                .Property(d => d.PrezzoUnitario)
                .HasColumnType("DECIMAL(10,2)");

            // Configurazione delle relazioni tra entità
            modelBuilder.Entity<Ordine>()
                .HasOne(o => o.Cliente)
                .WithMany(c => c.Ordini)
                .HasForeignKey(o => o.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ordine>()
                .HasIndex(o => o.IdCliente);

            // Configurazione dell'entità Utente
            modelBuilder.Entity<Utente>(entity =>
            {
                // Imposta la chiave primaria
                entity.HasKey(u => u.Id);

                // Imposta l'Username come unico
                entity.HasIndex(u => u.Username)
                      .IsUnique();

                // Configurazione delle colonne
                entity.Property(u => u.Username)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(u => u.PasswordHash)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(u => u.Role)
                      .IsRequired()
                      .HasMaxLength(50);
            });


           
        }
    }
}
