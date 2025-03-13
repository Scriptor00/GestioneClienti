using Microsoft.EntityFrameworkCore;
using WebAppEF.Entities;

namespace WebAppEF.Models
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Cliente> Clienti { get; set; }
        public DbSet<Prodotto> Prodotti { get; set; }
        public DbSet<Ordine> Ordini { get; set; }
        public DbSet<DettagliOrdine> DettagliOrdini { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
        }
    }
}