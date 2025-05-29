using Microsoft.AspNetCore.SignalR;

namespace ProgettoStage.Hubs
{
    public class DisponibilitaHub : Hub
    {
        public async Task AggiornaDisponibilita(string prodottoId, int nuovaQuantita)
        {
            await Clients.Others.SendAsync("RiceviAggiornamento", prodottoId, nuovaQuantita);
        }
    }
}

//Clients.Others invia il messaggio a tutti i client connessi tranne quello che ha inviato il messaggio originale.