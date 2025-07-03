using GestioneClienti.Entities;
using Microsoft.EntityFrameworkCore;
using ProgettoStage.Entities;
using WebAppEF.Entities;

namespace WebAppEF.Models
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {

        public DbSet<Cliente> Clienti { get; set; }
        public DbSet<Prodotto> Prodotti { get; set; }
        public DbSet<Ordine> Ordini { get; set; }
        public DbSet<DettagliOrdine> DettagliOrdini { get; set; }
        public DbSet<Utente> Utenti { get; set; }

        public DbSet<Carrello> Carrelli { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Carrello>()
              .HasOne(c => c.Utente)           // Un carrello ha un Utente
              .WithMany()                      // Un Utente può avere molti carrelli (senza una navigation property in Utente)
                                               // OPPURE .WithMany(u => u.Carrelli) se aggiungi ICollection<Carrello> Carrelli { get; set; } nell'entità Utente
              .HasForeignKey(c => c.IdUtente); // La chiave esterna è IdUtente in Carrello

            // Se in futuro volessi una lista di carrelli nell'entità Utente, aggiungi:
            // public ICollection<Carrello> Carrelli { get; set; } = new List<Carrello>();
            // alla tua entità Utente, e cambia .WithMany() in .WithMany(u => u.Carrelli)

            // Configura la relazione uno-a-molti tra Prodotto e Carrello
            modelBuilder.Entity<Carrello>()
                .HasOne(c => c.Prodotto)
                .WithMany() // O WithMany(p => p.Carrelli) se hai la navigation property in Prodotto
                .HasForeignKey(c => c.IdProdotto);



            modelBuilder.Entity<Prodotto>()
                .Property(p => p.Prezzo)
                .HasColumnType("DECIMAL(10,2)");

            modelBuilder.Entity<Ordine>()
                .Property(o => o.TotaleOrdine)
                .HasColumnType("DECIMAL(10,2)");

            modelBuilder.Entity<DettagliOrdine>()
                .Property(d => d.PrezzoUnitario)
                .HasColumnType("DECIMAL(10,2)");

            modelBuilder.Entity<Ordine>()
                .HasOne(o => o.Cliente)
                .WithMany(c => c.Ordini)
                .HasForeignKey(o => o.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ordine>()
                .HasIndex(o => o.IdCliente);


            modelBuilder.Entity<Utente>(entity =>
            {

                entity.HasKey(u => u.Id);


                entity.HasIndex(u => u.Username)
                      .IsUnique();


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

