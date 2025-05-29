using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebAppEF.Models; // Per ApplicationDbContext
using ProgettoStage.Entities; // Per Carrello, Ordine, DettagliOrdine, StatoOrdine, Prodotto
using ProgettoStage.DTOs; // Assicurati che il DTO CartUpdateDto sia qui o in una sua cartella DTOs
using ProgettoStage.Hubs;
using WebAppEF.Entities; // Per ArticoloOrdineRichiesta, assicurati che Prodotto sia in WebAppEF.Entities
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // Per List<T>
using System.Linq; // Per LINQ

namespace ProgettoStage.Services
{
    public class GestoreDisponibilitaProdotto
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly IHubContext<DisponibilitaHub> _contestoHub;
        private readonly ILogger<GestoreDisponibilitaProdotto> _logger;

        public GestoreDisponibilitaProdotto(
            IDbContextFactory<ApplicationDbContext> dbContextFactory,
            IHubContext<DisponibilitaHub> contestoHub,
            ILogger<GestoreDisponibilitaProdotto> logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _contestoHub = contestoHub ?? throw new ArgumentNullException(nameof(contestoHub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Ottiene la disponibilità complessiva di un prodotto, la quantità totale prenotata nei carrelli
        /// e la giacenza reale dal magazzino.
        /// </summary>
        /// <param name="idProdotto">L'ID del prodotto.</param>
        /// <returns>Una tupla contenente (giacenzaTotaleMagazzino, quantitàTotalePrenotataInTuttiICarrelli, giacenzaTotaleMagazzino).</returns>
        public async Task<(int disponibilitaComplessiva, int quantitaTotalePrenotata, int giacenzaReale)> OttieniInfoDisponibilitaProdotto(int idProdotto)
        {
            using (var contestoDB = await _dbContextFactory.CreateDbContextAsync())
            {
                var prodotto = await contestoDB.Prodotti.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdProdotto == idProdotto);

                if (prodotto == null)
                {
                    _logger.LogWarning($"Prodotto con ID {idProdotto} non trovato durante il recupero delle informazioni di disponibilità. Si assume stock 0.");
                    return (0, 0, 0);
                }

                int giacenzaTotale = prodotto.Giacenza; // Questa è la giacenza fisica totale dal DB.

                // Calcola la quantità totale di questo prodotto attualmente in TUTTI i carrelli (di tutti gli utenti).
                int quantitaPrenotataInCarrelli = await contestoDB.Carrelli
                    .Where(c => c.IdProdotto == idProdotto)
                    .SumAsync(c => c.Quantita);

                // --- MODIFICA CHIAVE QUI ---
                // Il primo valore della tupla ("disponibilitaComplessiva") è ora la giacenza totale fisica,
                // che il frontend (e SignalR) può usare per mostrare lo stock grezzo.
                // Il terzo valore ("giacenzaReale") è anch'esso la giacenza totale fisica.
                // La logica per calcolare la disponibilità *ordinabile per l'utente specifico*
                // viene gestita nel CarrelloController.
                return (giacenzaTotale, quantitaPrenotataInCarrelli, giacenzaTotale);
            }
        }

        /// <summary>
        /// Aggiorna la disponibilità di un prodotto e notifica tutti i client SignalR.
        /// </summary>
        /// <param name="idProdotto">L'ID del prodotto da aggiornare.</param>
        public async Task AggiornaENotificaDisponibilitaProdotto(int idProdotto)
        {
            // Qui OttieniInfoDisponibilitaProdotto restituirà (giacenzaTotale, quantitaTotalePrenotataInTuttiICarrelli, giacenzaTotale)
            var (disponibilitaComplessiva, quantitaTotalePrenotata, giacenzaReale) = await OttieniInfoDisponibilitaProdotto(idProdotto);
            _logger.LogInformation($"Notifica SignalR per Prodotto {idProdotto}: Disponibile Complessivo (Giacenza): {disponibilitaComplessiva}, Totale Prenotato in Carrelli: {quantitaTotalePrenotata}, Giacenza Reale (usato per calcoli): {giacenzaReale}");
            
            // Invia l'aggiornamento a tutti i client connessi al SignalR Hub
            // I valori inviati sono la giacenza totale, la quantità totale prenotata in carrelli e la giacenza reale (che è la stessa della giacenza totale)
            await _contestoHub.Clients.All.SendAsync("ReceiveProductAvailabilityUpdate", idProdotto, disponibilitaComplessiva, quantitaTotalePrenotata, giacenzaReale);
        }

        /// <summary>
        /// Aggiunge o aggiorna un articolo nel carrello di un utente, con validazione della disponibilità.
        /// Gestisce la concorrenza tramite transazioni e DbUpdateConcurrencyException.
        /// </summary>
        /// <param name="idUtente">L'ID dell'utente.</param>
        /// <param name="idProdotto">L'ID del prodotto.</param>
        /// <param name="quantita">La quantità desiderata (0 per rimuovere).</param>
        public async Task AggiungiOAggiornaArticoloCarrello(int idUtente, int idProdotto, int quantita)
        {
            using (var contestoDB = await _dbContextFactory.CreateDbContextAsync())
            {
                using (var transaction = await contestoDB.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var prodotto = await contestoDB.Prodotti
                            .FirstOrDefaultAsync(p => p.IdProdotto == idProdotto);

                        if (prodotto == null)
                        {
                            _logger.LogWarning($"Tentativo di aggiungere/aggiornare un prodotto inesistente (ID: {idProdotto}) al carrello dell'utente {idUtente}.");
                            throw new InvalidOperationException($"Prodotto con ID {idProdotto} non trovato.");
                        }

                        var articoloCarrello = await contestoDB.Carrelli
                            .FirstOrDefaultAsync(c => c.IdUtente == idUtente && c.IdProdotto == idProdotto); 
                        
                        int oldQuantityInCart = articoloCarrello?.Quantita ?? 0;
                        int newQuantity = quantita; 

                        // Caso: La quantità richiesta è zero o negativa, quindi rimuovi l'articolo dal carrello
                        if (newQuantity <= 0)
                        {
                            if (oldQuantityInCart > 0)
                            {
                                contestoDB.Carrelli.Remove(articoloCarrello);
                                _logger.LogInformation($"Rimosso prodotto {idProdotto} dal carrello dell'utente {idUtente} a causa di un aggiornamento a quantità zero o negativa.");
                            }
                            else
                            {
                                _logger.LogInformation($"Saltata l'aggiunta al carrello per il prodotto {idProdotto} con quantità zero o negativa per l'utente {idUtente}. Non era presente.");
                            }
                            await contestoDB.SaveChangesAsync();
                            await transaction.CommitAsync();
                            await AggiornaENotificaDisponibilitaProdotto(idProdotto);
                            return; // Terminata l'operazione
                        }
                        
                        // Ottieni le informazioni di disponibilità aggiornate
                        // giacenzaReale: stock fisico
                        // totalBookedQuantity: quantità in TUTTI i carrelli
                        var (overallAvailableQuantity, totalBookedQuantity, giacenzaReale) = await OttieniInfoDisponibilitaProdotto(idProdotto);
                        
                        // Calcola la quantità già prenotata da *altri* utenti (escludendo la mia vecchia quantità)
                        int bookedByOthers = Math.Max(0, totalBookedQuantity - oldQuantityInCart);
                        
                        // Calcola la quantità massima che questo utente può aggiungere/avere nel carrello
                        int maxQuantityThisUserCanHave = giacenzaReale - bookedByOthers;

                        // Validazione: la nuova quantità richiesta non deve superare la massima disponibile per l'utente
                        if (newQuantity > maxQuantityThisUserCanHave)
                        {
                            throw new InvalidOperationException($"Quantità richiesta ({newQuantity}) per '{prodotto.NomeProdotto}' supera la disponibilità massima che puoi avere nel carrello ({maxQuantityThisUserCanHave}).");
                        }

                        // Aggiorna o aggiungi l'articolo nel carrello
                        if (articoloCarrello == null)
                        {
                            contestoDB.Carrelli.Add(new Carrello
                            {
                                IdUtente = idUtente, 
                                IdProdotto = idProdotto,
                                Quantita = newQuantity,
                                DataUltimoAggiornamento = DateTime.Now
                            });
                            _logger.LogInformation($"Aggiunto prodotto {idProdotto} con quantità {newQuantity} al carrello dell'utente {idUtente}.");
                        }
                        else
                        {
                            articoloCarrello.Quantita = newQuantity;
                            articoloCarrello.DataUltimoAggiornamento = DateTime.Now; 
                            contestoDB.Carrelli.Update(articoloCarrello);
                            _logger.LogInformation($"Aggiornata la quantità del prodotto {idProdotto} a {newQuantity} nel carrello dell'utente {idUtente}.");
                        }

                        await contestoDB.SaveChangesAsync(); 
                        await transaction.CommitAsync(); 
                        await AggiornaENotificaDisponibilitaProdotto(idProdotto); // Notifica i client SignalR dell'aggiornamento
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        await transaction.RollbackAsync(); 
                        _logger.LogError(ex, $"Errore di concorrenza durante l'aggiornamento del carrello per utente {idUtente}, prodotto {idProdotto}. Un altro utente ha modificato il prodotto.");
                        throw new InvalidOperationException("Il prodotto che stai cercando di modificare è stato aggiornato da un altro utente. Si prega di ricaricare la pagina e riprovare.", ex);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(); 
                        _logger.LogError(ex, $"Errore durante l'aggiunta/aggiornamento dell'articolo nel carrello per utente {idUtente}, prodotto {idProdotto}.");
                        throw; 
                    }
                }
            }
        }

        /// <summary>
        /// Rimuove un articolo dal carrello di un utente.
        /// </summary>
        /// <param name="idUtente">L'ID dell'utente (int).</param>
        /// <param name="idProdotto">L'ID del prodotto da rimuovere.</param>
        public async Task RimuoviArticoloCarrello(int idUtente, int idProdotto) 
        {
            using (var contestoDB = await _dbContextFactory.CreateDbContextAsync())
            {
                using (var transaction = await contestoDB.Database.BeginTransactionAsync()) 
                {
                    try
                    {
                        var articoloCarrello = await contestoDB.Carrelli
                            .FirstOrDefaultAsync(c => c.IdUtente == idUtente && c.IdProdotto == idProdotto); 

                        if (articoloCarrello != null)
                        {
                            contestoDB.Carrelli.Remove(articoloCarrello);
                            await contestoDB.SaveChangesAsync();
                            _logger.LogInformation($"Rimosso prodotto {idProdotto} dal carrello dell'utente {idUtente}.");
                        }
                        else
                        {
                            _logger.LogWarning($"Tentativo di rimuovere un prodotto inesistente ({idProdotto}) dal carrello dell'utente {idUtente}.");
                        }
                        await transaction.CommitAsync(); 

                        await AggiornaENotificaDisponibilitaProdotto(idProdotto); // Notifica i client SignalR
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, $"Errore di concorrenza durante la rimozione dal carrello per utente {idUtente}, prodotto {idProdotto}.");
                        throw new InvalidOperationException("Un altro utente ha modificato le informazioni relative a questo prodotto. Si prega di riprovare.", ex);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, $"Errore durante la rimozione dal carrello per utente {idUtente}, prodotto {idProdotto}.");
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Controlla se un ordine può essere piazzato in base alla disponibilità del prodotto.
        /// Questo metodo è chiamato prima di processare l'ordine per una verifica finale.
        /// </summary>
        /// <param name="idUtenteCorrente">L'ID dell'utente che sta piazzando l'ordine.</param>
        /// <param name="articoliOrdine">La lista degli articoli dell'ordine.</param>
        /// <returns>Una tupla (successo, messaggio) che indica se l'ordine può essere piazzato e un messaggio descrittivo.</returns>
        public async Task<(bool successo, string messaggio)> PuoPiazzareOrdine(int idUtenteCorrente, List<ArticoloOrdineRichiesta> articoliOrdine)
        {
            using (var contestoDB = await _dbContextFactory.CreateDbContextAsync())
            {
                foreach (var articolo in articoliOrdine)
                {
                    var prodotto = await contestoDB.Prodotti.AsNoTracking().FirstOrDefaultAsync(p => p.IdProdotto == articolo.ProdottoId);
                    if (prodotto == null)
                    {
                        return (false, $"Errore: Prodotto con ID {articolo.ProdottoId} non trovato.");
                    }

                    int giacenzaReale = prodotto.Giacenza; // Giacenza totale fisica

                    // Quantità prenotata da *altri* carrelli (escludendo l'utente corrente)
                    var quantitaPrenotataDaAltri = await contestoDB.Carrelli
                        .Where(c => c.IdProdotto == articolo.ProdottoId && c.IdUtente != idUtenteCorrente) 
                        .SumAsync(c => c.Quantita);

                    int disponibilitaEffettivaPerUtente = giacenzaReale - quantitaPrenotataDaAltri;

                    // Verifica se la quantità richiesta dall'ordine supera la disponibilità effettiva per l'utente
                    if (articolo.Quantita > disponibilitaEffettivaPerUtente)
                    {
                        return (false, $"Quantità insufficiente per il prodotto '{prodotto.NomeProdotto ?? "N/D"}'. Disponibile: {disponibilitaEffettivaPerUtente}, Richiesto: {articolo.Quantita}. Si prega di rivedere il carrello.");
                    }
                }
                _logger.LogInformation("Controllo pre-ordine completato con successo. Le quantità sembrano disponibili.");
                return (true, "Controllo pre-ordine completato con successo. Le quantità sembrano disponibili.");
            }
        }

        /// <summary>
        /// Processa un ordine, aggiornando la giacenza e svuotando gli articoli dal carrello.
        /// Implementa gestione della concorrenza per l'aggiornamento della giacenza.
        /// </summary>
        /// <param name="idUtente">L'ID dell'utente che sta piazzando l'ordine (int).</param>
        /// <param name="articoliOrdine">La lista degli articoli dell'ordine.</param>
        public async Task ProcessaOrdine(int idUtente, List<ArticoloOrdineRichiesta> articoliOrdine) 
        {
            using (var contestoDB = await _dbContextFactory.CreateDbContextAsync())
            {
                using (var transazione = await contestoDB.Database.BeginTransactionAsync())
                {
                    try
                    {
                        var nuovoOrdine = new Ordine
                        {
                            IdCliente = idUtente, 
                            DataOrdine = DateTime.Now,
                            Stato = StatoOrdine.Confermato, // Assicurati che StatoOrdine sia un enum o simile
                            TotaleOrdine = 0m
                        };

                        contestoDB.Ordini.Add(nuovoOrdine);
                        await contestoDB.SaveChangesAsync(); // Salva l'ordine per ottenere il suo IdOrdine

                        decimal totaleCalcolato = 0m;
                        List<int> prodottiAggiornati = new List<int>(); 

                        foreach (var articolo in articoliOrdine)
                        {
                            var prodotto = await contestoDB.Prodotti.FirstOrDefaultAsync(p => p.IdProdotto == articolo.ProdottoId);
                            if (prodotto == null)
                            {
                                throw new InvalidOperationException($"Prodotto con ID {articolo.ProdottoId} non trovato durante l'elaborazione dell'ordine.");
                            }

                            // Verifica finale della giacenza prima di sottrarre
                            if (prodotto.Giacenza < articolo.Quantita)
                            {
                                throw new InvalidOperationException($"Quantità insufficiente per il prodotto '{prodotto.NomeProdotto}'. Disponibile: {prodotto.Giacenza}, Richiesto: {articolo.Quantita}. Ordine non completato.");
                            }

                            prodotto.Giacenza -= articolo.Quantita; // Riduci la giacenza
                            contestoDB.Prodotti.Update(prodotto); // Segna il prodotto come modificato
                            totaleCalcolato += prodotto.Prezzo * articolo.Quantita;

                            contestoDB.DettagliOrdini.Add(new DettagliOrdine
                            {
                                IdOrdine = nuovoOrdine.IdOrdine,
                                IdProdotto = articolo.ProdottoId,
                                Quantita = articolo.Quantita,
                                PrezzoUnitario = prodotto.Prezzo
                            });
                            prodottiAggiornati.Add(articolo.ProdottoId);
                        }
                        nuovoOrdine.TotaleOrdine = totaleCalcolato;
                        contestoDB.Update(nuovoOrdine); // Aggiorna il totale dell'ordine

                        // Rimuovi gli articoli dal carrello dell'utente una volta ordinati
                        var articoliCarrelloDaRimuovere = await contestoDB.Carrelli
                            .Where(c => c.IdUtente == idUtente && articoliOrdine.Select(oi => oi.ProdottoId).Contains(c.IdProdotto)) 
                            .ToListAsync();
                        contestoDB.Carrelli.RemoveRange(articoliCarrelloDaRimuovere);

                        await contestoDB.SaveChangesAsync(); // Salva tutte le modifiche (giacenze, dettagli ordine, carrello)
                        await transazione.CommitAsync(); // Conferma la transazione

                        _logger.LogInformation($"Ordine {nuovoOrdine.IdOrdine} elaborato con successo per l'utente {idUtente}.");

                        // Notifica gli aggiornamenti di disponibilità per i prodotti coinvolti nell'ordine
                        foreach (var idProdottoAggiornato in prodottiAggiornati.Distinct())
                        {
                            await AggiornaENotificaDisponibilitaProdotto(idProdottoAggiornato);
                        }
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        await transazione.RollbackAsync(); 
                        _logger.LogError(ex, $"Errore di concorrenza durante l'elaborazione dell'ordine per l'utente {idUtente}. Uno o più prodotti sono stati modificati da un altro utente.");
                        throw new InvalidOperationException("Uno o più prodotti nel tuo ordine sono stati aggiornati da un altro utente. Si prega di ricaricare la pagina e riprovare a completare l'ordine.", ex);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Errore durante l'elaborazione dell'ordine per l'utente {idUtente}. Rollback della transazione.");
                        await transazione.RollbackAsync();
                        throw;
                    }
                }
            }
        }
    }
}