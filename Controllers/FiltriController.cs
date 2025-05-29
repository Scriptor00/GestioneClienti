using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppEF.Entities;
using WebAppEF.Models;
using WebAppEF.ViewModel;

namespace WebAppEF.Controllers
{
    public class FiltriController(ApplicationDbContext context, ILogger<FiltriController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<FiltriController> _logger = logger;

        [HttpGet]
        public IActionResult Filtri()
        {
            var viewModel = new RisultatiRicercaViewModel();
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetClientiSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<object>());
            }

            var clienti = await _context.Clienti
                .Where(c => c.Nome.Contains(term) || c.Cognome.Contains(term))
                .Select(c => new { label = $"{c.Nome} {c.Cognome}", value = c.IdCliente })
                .ToListAsync();

            return Json(clienti);
        }

        [HttpGet]
        public IActionResult RicercaRisultati(string nomeCliente, int? idOrdine)
        {
            _logger.LogInformation("Inizio ricerca: NomeCliente={NomeCliente}, IdOrdine={IdOrdine}", nomeCliente, idOrdine);

            var viewModel = new RisultatiRicercaViewModel
            {
                NomeCliente = nomeCliente,
                IdOrdine = idOrdine
            };

            try
            {
                var clientiQuery = _context.Clienti
                    .Include(c => c.Ordini)
                    .AsQueryable();

                var ordiniQuery = _context.Ordini
                    .Include(o => o.Cliente)
                    .AsQueryable();

                // Filtraggio per nome cliente
                if (!string.IsNullOrEmpty(nomeCliente))
                {
                    var nomeClienteLower = nomeCliente.ToLower();
                    _logger.LogInformation("Filtraggio per nome cliente: {NomeClienteLower}", nomeClienteLower);

                    var nomeCognomeSeparati = nomeCliente.Split(' ');

                    if (nomeCognomeSeparati.Length == 2)
                    {
                        var nome = nomeCognomeSeparati[0].ToLower();
                        var cognome = nomeCognomeSeparati[1].ToLower();

                        _logger.LogInformation("Ricerca per nome={Nome} e cognome={Cognome}", nome, cognome);

                        clientiQuery = clientiQuery.Where(c =>
                            EF.Functions.Like(c.Nome.ToLower(), "%" + nome + "%") &&
                            EF.Functions.Like(c.Cognome.ToLower(), "%" + cognome + "%"));

                        ordiniQuery = ordiniQuery.Where(o =>
                            EF.Functions.Like(o.Cliente.Nome.ToLower(), "%" + nome + "%") &&
                            EF.Functions.Like(o.Cliente.Cognome.ToLower(), "%" + cognome + "%"));
                    }
                    else
                    {
                        _logger.LogInformation("Ricerca per nome o cognome contenente: {NomeClienteLower}", nomeClienteLower);

                        clientiQuery = clientiQuery.Where(c =>
                            EF.Functions.Like(c.Nome.ToLower(), "%" + nomeClienteLower + "%") ||
                            EF.Functions.Like(c.Cognome.ToLower(), "%" + nomeClienteLower + "%"));

                        ordiniQuery = ordiniQuery.Where(o =>
                            EF.Functions.Like(o.Cliente.Nome.ToLower(), "%" + nomeClienteLower + "%") ||
                            EF.Functions.Like(o.Cliente.Cognome.ToLower(), "%" + nomeClienteLower + "%"));
                    }
                }

                // Filtraggio per ID Ordine
                if (idOrdine.HasValue)
                {
                    _logger.LogInformation("Filtraggio per ID Ordine: {IdOrdine}", idOrdine.Value);

                    ordiniQuery = ordiniQuery.Where(o => o.IdOrdine == idOrdine.Value);
                    clientiQuery = clientiQuery.Where(c => c.Ordini != null && c.Ordini.Any(o => o.IdOrdine == idOrdine.Value));

                    var ordine = ordiniQuery.SingleOrDefault(o => o.IdOrdine == idOrdine.Value);
                    if (ordine != null && ordine.Cliente != null)
                    {
                        _logger.LogInformation("Ordine trovato: {IdOrdine}, Cliente: {ClienteNome} {ClienteCognome}",
                            ordine.IdOrdine, ordine.Cliente.Nome, ordine.Cliente.Cognome);

                        viewModel.Ordini = new List<Ordine> { ordine };
                        viewModel.Clienti = new List<Cliente> { ordine.Cliente };
                    }
                    else
                    {
                        _logger.LogWarning("Nessun ordine trovato con ID: {IdOrdine}", idOrdine.Value);

                        viewModel.Ordini = new List<Ordine>();
                        viewModel.Clienti = new List<Cliente>();
                    }

                    return View("Risultati", viewModel);
                }

                // Se nessun filtro è stato applicato, restituisci tutti i clienti e gli ordini
                if (string.IsNullOrEmpty(nomeCliente) && !idOrdine.HasValue)
                {
                    viewModel.Clienti = clientiQuery.ToList();
                    viewModel.Ordini = ordiniQuery.ToList();

                    _logger.LogInformation("Nessun filtro applicato. Restituiti tutti i clienti e gli ordini: {NumeroClienti} clienti, {NumeroOrdini} ordini.",
                        viewModel.Clienti.Count, viewModel.Ordini.Count);
                }
                else
                {
                    // Se ci sono filtri, restituisci i risultati filtrati
                    viewModel.Ordini = ordiniQuery.ToList();
                    viewModel.Clienti = clientiQuery.ToList();

                    _logger.LogInformation("Risultati della ricerca: {NumeroClienti} clienti trovati, {NumeroOrdini} ordini trovati.",
                        viewModel.Clienti.Count, viewModel.Ordini.Count);
                }

                return View("Risultati", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la ricerca NomeCliente={NomeCliente}, IdOrdine={IdOrdine}", nomeCliente, idOrdine);
                return View("Errore");
            }
        }

        [HttpGet]
        public IActionResult GetOrdiniCliente(int idCliente)
        {
            var ordini = _context.Ordini
                .Where(o => Convert.ToInt32(o.IdCliente) == idCliente)
                .Select(o => new
                {
                    o.IdOrdine,
                    o.DataOrdine,
                    o.TotaleOrdine,
                    o.Stato
                })
                .ToList();

            return Json(ordini);
        }
    }
}