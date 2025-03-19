using WebAppEF.Entities;
using System;
using System.Linq;
using WebAppEF.Models;

namespace WebAppEF.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Verifica se il database è già popolato
            if (context.Prodotti.Any())
            {
                return; // Il database è già stato popolato
            }

            // Crea una lista di prodotti di gaming
            var prodotti = new Prodotto[]
            {
                new() {
                    NomeProdotto = "PlayStation 5",
                    Categoria = "Console",
                    Prezzo = 499.99m,
                    Giacenza = 10,
                    DataInserimento = DateTime.Now,
                    ImmagineUrl = "https://wallpapercave.com/wp/wp6803047.jpg" // Immagine PlayStation 5
                },
                new() {
                    NomeProdotto = "Xbox Series X",
                    Categoria = "Console",
                    Prezzo = 499.99m,
                    Giacenza = 8,
                    DataInserimento = DateTime.Now,
                    ImmagineUrl = "https://images.pexels.com/photos/12401185/pexels-photo-12401185.jpeg?auto=compress&cs=tinysrgb&w=1260&h=750&dpr=2" // Immagine Xbox Series X
                },
                new() {
                    NomeProdotto = "Nintendo Switch",
                    Categoria = "Console",
                    Prezzo = 299.99m,
                    Giacenza = 15,
                    DataInserimento = DateTime.Now,
                    ImmagineUrl = "https://images.pexels.com/photos/371924/pexels-photo-371924.jpeg?auto=compress&cs=tinysrgb&w=600" // Immagine Nintendo Switch
                },
                new() {
                    NomeProdotto = "Logitech G Pro X",
                    Categoria = "Accessori",
                    Prezzo = 129.99m,
                    Giacenza = 20,
                    DataInserimento = DateTime.Now,
                    ImmagineUrl = "https://images.pexels.com/photos/18966448/pexels-photo-18966448/free-photo-of-logitech-g432-gaming-headset-next-to-a-controller.jpeg?auto=compress&cs=tinysrgb&w=600" // Immagine Logitech G Pro X
                },
                new() {
                    NomeProdotto = "Razer DeathAdder V2",
                    Categoria = "Accessori",
                    Prezzo = 69.99m,
                    Giacenza = 25,
                    DataInserimento = DateTime.Now,
                    ImmagineUrl = "https://th.bing.com/th/id/OIP.S7EQ8Yb7k0zGp2YhBvZ61AHaFj?w=257&h=192&c=7&r=0&o=5&dpr=1.5&pid=1.7" // Immagine Razer DeathAdder V2
                },
                new() {
                    NomeProdotto = "Cyberpunk 2077",
                    Categoria = "Giochi",
                    Prezzo = 59.99m,
                    Giacenza = 50,
                    DataInserimento = DateTime.Now,
                    ImmagineUrl = "https://s1.1zoom.me/b5050/297/Cyberpunk_2077_Pistols_Men_Jacket_Cyborg_566009_1366x768.jpg" // Immagine Cyberpunk 2077
                },
                new() {
                    NomeProdotto = "The Legend of Zelda: Breath of the Wild",
                    Categoria = "Giochi",
                    Prezzo = 49.99m,
                    Giacenza = 30,
                    DataInserimento = DateTime.Now,
                    ImmagineUrl = "https://th.bing.com/th/id/OIP.HLliURS7N4XlazBHBL_YYQHaDd?w=342&h=163&c=7&r=0&o=5&dpr=1.5&pid=1.7" // Immagine Zelda Breath of the Wild
                }
            };

            // Aggiungi i prodotti al database
            context.Prodotti.AddRange(prodotti);
            context.SaveChanges();
        }
    }
}
