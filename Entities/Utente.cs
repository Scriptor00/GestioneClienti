using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestioneClienti.Entities
{

    public class Utente
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string Role { get; set; }
    
    // Aggiungiamo la collezione di notifiche
    public ICollection<Notifica> Notifiche { get; set; } = new List<Notifica>();
}
}
