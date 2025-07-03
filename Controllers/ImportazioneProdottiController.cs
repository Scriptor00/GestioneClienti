using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using WebAppEF.Entities;
using Microsoft.EntityFrameworkCore;
using WebAppEF.Models;

namespace ProgettoStage.Controllers
{
    public class ImportazioneProdottiController : Controller
    {
        private readonly ApplicationDbContext _dbContext;

        private const string ExcelDataSessionKey = "ExcelDataProdotti";
        private const string FileNameSessionKey = "FileNameProdotti";
        private const string MappingTempDataKey = "MappingProdotti";
        private const string ImportErrorsSessionKey = "ImportErrorsProdotti";

        public ImportazioneProdottiController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: ImportazioneProdotti/Index
        // Mostra la pagina di upload iniziale per i file Excel dei prodotti.
        [HttpGet]
        public IActionResult Index()
        {
            // Pulisce eventuali dati di sessione relativi a importazioni precedenti all'inizio di una nuova.
            HttpContext.Session.Remove(ExcelDataSessionKey);
            HttpContext.Session.Remove(FileNameSessionKey);
            HttpContext.Session.Remove(ImportErrorsSessionKey);
            return View("UploadExcelProdotti");
        }

        // POST: ImportazioneProdotti/Mapping
        // Gestisce l'upload del file Excel, legge le intestazioni e reindirizza alla pagina di mapping.
        [HttpPost]
        [ValidateAntiForgeryToken] // Protezione CSRF
        public IActionResult Mapping(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Errore"] = "Seleziona un file Excel valido per l'importazione dei prodotti.";
                return RedirectToAction("Index");
            }

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Errore"] = "Il file deve essere in formato .xlsx.";
                return RedirectToAction("Index");
            }

            try
            {
                using var stream = new MemoryStream();
                // Copia il contenuto del file caricato nello stream di memoria
                file.CopyTo(stream);
                // Riposiziona lo stream all'inizio per la lettura con ClosedXML
                stream.Position = 0;

                // Apri il workbook dal MemoryStream usando ClosedXML
                using var workbook = new XLWorkbook(stream);
                // Ottieni il primo foglio di lavoro (ClosedXML usa l'indice 1 per il primo foglio)
                var ws = workbook.Worksheet(1);

                // Legge le intestazioni dalla prima riga del foglio Excel, filtrando quelle vuote
                var intestazioniExcel = ws.Row(1).CellsUsed()
                                                .Where(c => !string.IsNullOrWhiteSpace(c.Value.ToString()))
                                                .Select(c => c.Value.ToString().Trim())
                                                .ToList();

                // Se non vengono trovate intestazioni, reindirizza con un errore
                if (!intestazioniExcel.Any())
                {
                    TempData["Errore"] = "Il file Excel non contiene intestazioni valide nella prima riga.";
                    return RedirectToAction("Index");
                }

                // Passa le intestazioni delle colonne Excel alla vista tramite ViewBag per il mapping
                ViewBag.ColonneExcel = intestazioniExcel;

                // Salva l'array di byte del file Excel e il nome del file nella sessione
                // per poterli recuperare nell'azione Importa
                HttpContext.Session.Set(ExcelDataSessionKey, stream.ToArray());
                HttpContext.Session.SetString(FileNameSessionKey, file.FileName);
                HttpContext.Session.Remove(ImportErrorsSessionKey); // Pulisce eventuali errori di una sessione precedente

                return View("MappingProdotti");
            }
            catch (Exception ex)
            {
                TempData["Errore"] = $"Si è verificato un errore durante la lettura del file Excel: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: ImportazioneProdotti/SalvaMapping
        // Riceve il mapping delle colonne dalla form, lo salva in TempData e reindirizza all'azione Importa.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SalvaMapping([FromForm] Dictionary<string, string> mappings)
        {
            // Verifica che siano stati selezionati dei mapping
            if (mappings == null || !mappings.Any())
            {
                TempData["Errore"] = "Nessun mapping delle colonne è stato selezionato.";
                return RedirectToAction("Index");
            }

            // Serializza il dizionario di mapping in JSON e salvalo in TempData.
            // TempData è usato perché i dati devono persistere per il successivo RedirectToAction.
            TempData[MappingTempDataKey] = JsonSerializer.Serialize(mappings);

            return RedirectToAction("Importa");
        }

        // GET: ImportazioneProdotti/Importa
        // Esegue l'importazione dei dati dei prodotti dal file Excel nel database,
        // basandosi sui mapping e sul file salvati in sessione/TempData.
        [HttpGet]
        public async Task<IActionResult> Importa()
        {
            // Recupera i dati necessari dalla sessione e TempData
            var mappingJson = TempData[MappingTempDataKey] as string;
            byte[] excelBytes = HttpContext.Session.Get(ExcelDataSessionKey);
            string fileName = HttpContext.Session.GetString(FileNameSessionKey);

            // Verifica che tutti i dati necessari per l'importazione siano disponibili
            if (string.IsNullOrEmpty(mappingJson) || excelBytes == null || excelBytes.Length == 0)
            {
                TempData["Errore"] = "Impossibile recuperare i dati per l'importazione. Riprova l'upload e il mapping del file prodotti.";
                return RedirectToAction("Index");
            }

            // Deserializza il dizionario di mapping JSON
            var mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(mappingJson);

            var importedProducts = new List<Prodotto>(); // Lista per i prodotti importati con successo
            var importErrorsLog = new List<string>();    // Lista per i messaggi di errore

            try
            {
                // Crea un MemoryStream dall'array di byte recuperato e apri il workbook
                using var stream = new MemoryStream(excelBytes);
                stream.Position = 0; // Riposiziona lo stream all'inizio per la lettura
                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheet(1); // Ottieni il primo foglio di lavoro

                var headerRow = ws.Row(1);
                var dataRows = ws.RowsUsed().Skip(1); // Salta la riga dell'intestazione per leggere solo i dati

                // Itera su ogni riga di dati nel foglio Excel
                foreach (var row in dataRows)
                {
                    var prodotto = new Prodotto(); // Crea una nuova istanza di Prodotto per ogni riga
                    var currentRowErrors = new List<string>(); // Lista per gli errori specifici di questa riga
                    bool recordSuccessfullyParsed = true;

                    // Ignora righe che sono completamente vuote (nessuna cella utilizzata contiene testo)
                    if (row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.Value.ToString())))
                    {
                        continue;
                    }

                    // Itera su tutti i mapping definiti dall'utente per popolare l'oggetto Prodotto
                    foreach (var mappingEntry in mappings)
                    {
                        var excelColumnName = mappingEntry.Key;     // Nome della colonna in Excel (es. "Nome Cliente")
                        var modelPropertyName = mappingEntry.Value; // Nome della proprietà nel modello Prodotto (es. "NomeProdotto")

                        // Se l'utente non ha mappato un campo specifico, lo saltiamo
                        if (string.IsNullOrWhiteSpace(modelPropertyName))
                        {
                            continue;
                        }

                        // Trova la cella dell'intestazione corrispondente al nome della colonna Excel
                        var excelColumnCell = headerRow.CellsUsed().FirstOrDefault(c => c.Value.ToString().Trim().Equals(excelColumnName, StringComparison.OrdinalIgnoreCase));
                        if (excelColumnCell == null)
                        {
                            // Questo non dovrebbe succedere se il mapping è stato creato correttamente,
                            // ma è una sicurezza.
                            currentRowErrors.Add($"Colonna Excel '{excelColumnName}' non trovata nel file per il mapping '{modelPropertyName}'.");
                            recordSuccessfullyParsed = false;
                            continue;
                        }

                        var excelColumnIndex = excelColumnCell.Address.ColumnNumber;
                        var cell = row.Cell(excelColumnIndex);
                        var cellValue = cell.GetValue<string>()?.Trim(); // Ottieni il valore come stringa e trimma, gestendo null

                        // Ottieni la proprietà del modello Prodotto tramite Reflection per impostarne il valore
                        var property = typeof(Prodotto).GetProperty(modelPropertyName);
                        if (property == null)
                        {
                            // Questo caso indica un mapping a una proprietà inesistente nel modello
                            currentRowErrors.Add($"Proprietà '{modelPropertyName}' non trovata nel modello Prodotto.");
                            recordSuccessfullyParsed = false;
                            continue;
                        }

                        // Gestione della conversione dei tipi di dato per ogni proprietà
                        try
                        {
                            // Gestisce i casi in cui il valore della cella è nullo o vuoto
                            if (string.IsNullOrWhiteSpace(cellValue))
                            {
                                // Se la proprietà è di tipo stringa o nullable (es. int?, DateTime?, bool?), impostala a null
                                if (Nullable.GetUnderlyingType(property.PropertyType) != null || property.PropertyType == typeof(string))
                                {
                                    property.SetValue(prodotto, null);
                                }
                                // Se è un tipo non-nullable e non è una stringa, è un errore di valore obbligatorio mancante
                                else if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
                                {
                                    currentRowErrors.Add($"Valore obbligatorio mancante per il campo '{modelPropertyName}' (riga {row.RowNumber()}).");
                                    recordSuccessfullyParsed = false;
                                }
                                continue; // Passa al prossimo mapping per questa riga
                            }

                            // Logica di conversione specifica per ogni tipo di dato del modello Prodotto
                            if (property.PropertyType == typeof(string))
                            {
                                property.SetValue(prodotto, cellValue);
                            }
                            else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
                            {
                                if (int.TryParse(cellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out int intValue))
                                {
                                    if (modelPropertyName == "Giacenza" && intValue < 0)
                                    {
                                        currentRowErrors.Add($"Valore '{cellValue}' non valido per il campo '{modelPropertyName}' (atteso numero intero non negativo).");
                                        recordSuccessfullyParsed = false;
                                    }
                                    else
                                    {
                                        property.SetValue(prodotto, intValue);
                                    }
                                }
                                else
                                {
                                    currentRowErrors.Add($"Valore '{cellValue}' non valido per il campo '{modelPropertyName}' (atteso numero intero).");
                                    recordSuccessfullyParsed = false;
                                }
                            }
                            else if (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?))
                            {
                                if (decimal.TryParse(cellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decimalValue))
                                {
                                    if (modelPropertyName == "Prezzo" && decimalValue <= 0) // FIX: Prezzo deve essere > 0
                                    {
                                        currentRowErrors.Add($"Valore '{cellValue}' non valido per il campo '{modelPropertyName}' (atteso numero decimale maggiore di zero).");
                                        recordSuccessfullyParsed = false;
                                    }
                                    else
                                    {
                                        property.SetValue(prodotto, decimalValue);
                                    }
                                }
                                else
                                {
                                    currentRowErrors.Add($"Valore '{cellValue}' non valido per il campo '{modelPropertyName}' (atteso numero decimale).");
                                    recordSuccessfullyParsed = false;
                                }
                            }
                            else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                            {
                                if (cell.DataType == XLDataType.DateTime)
                                {
                                    property.SetValue(prodotto, cell.GetDateTime());
                                }
                                else if (DateTime.TryParse(cellValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateValue))
                                {
                                    property.SetValue(prodotto, dateValue);
                                }
                                else if (DateTime.TryParseExact(cellValue, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue))
                                {
                                    property.SetValue(prodotto, dateValue);
                                }
                                else if (DateTime.TryParseExact(cellValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue))
                                {
                                    property.SetValue(prodotto, dateValue);
                                }
                                else
                                {
                                    currentRowErrors.Add($"Valore '{cellValue}' non valido per il campo '{modelPropertyName}' (attesa data valida).");
                                    recordSuccessfullyParsed = false;
                                }
                            }
                            // Puoi aggiungere altre conversioni di tipo qui (es. bool, Guid, ecc.)
                        }
                        catch (Exception ex)
                        {
                            currentRowErrors.Add($"Errore di conversione per '{modelPropertyName}' con valore '{cellValue}': {ex.Message}");
                            recordSuccessfullyParsed = false;
                        }
                    }

                    // Validazione aggiuntiva dopo aver processato tutti i campi mappati per il prodotto
                    if (recordSuccessfullyParsed)
                    {
                        // Controllo di obbligatorietà per NomeProdotto
                        if (string.IsNullOrEmpty(prodotto.NomeProdotto))
                        {
                            currentRowErrors.Add("Il campo 'Nome Prodotto' è obbligatorio e non può essere vuoto.");
                            recordSuccessfullyParsed = false;
                        }
                        else
                        {
                            // Controllo duplicato per NomeProdotto
                            var existingProduct = await _dbContext.Prodotti.FirstOrDefaultAsync(p => p.NomeProdotto == prodotto.NomeProdotto);
                            if (existingProduct != null)
                            {
                                currentRowErrors.Add($"Nome prodotto '{prodotto.NomeProdotto}' già presente nel database. Il record non verrà importato.");
                                recordSuccessfullyParsed = false;
                            }
                        }
                    }

                    // Se il record è stato processato con successo E non ci sono stati errori (parsing, validazione o logica di business)
                    if (recordSuccessfullyParsed && !currentRowErrors.Any())
                    {
                        importedProducts.Add(prodotto); // Aggiungi il prodotto alla lista dei prodotti da importare
                    }
                    else // Se ci sono stati errori (parsing o duplicati)
                    {
                        // Prepara il contenuto originale della riga per il log degli errori
                        string rowContent = string.Join(", ", row.CellsUsed().Select(c => $"'{c.Value.ToString() ?? ""}'"));
                        importErrorsLog.Add($"Riga {row.RowNumber()} del file '{fileName}': Contenuto [{rowContent}] - Errori: {string.Join("; ", currentRowErrors)}");
                    }
                }

                // Salva tutti i prodotti validi nel database in un'unica operazione batch
                if (importedProducts.Any())
                {
                    _dbContext.Prodotti.AddRange(importedProducts);
                    await _dbContext.SaveChangesAsync();
                }

                // Pulisci i dati della sessione e TempData che non sono più necessari dopo l'importazione
                HttpContext.Session.Remove(ExcelDataSessionKey);
                HttpContext.Session.Remove(FileNameSessionKey);
                TempData.Remove(MappingTempDataKey);

                // Salva gli errori di importazione nella sessione, se presenti, per visualizzarli in una pagina dedicata
                if (importErrorsLog.Any())
                {
                    HttpContext.Session.SetString(ImportErrorsSessionKey, JsonSerializer.Serialize(importErrorsLog));
                }
                else
                {
                    // Se non ci sono errori, assicurati che la sessione sia pulita anche per gli errori
                    HttpContext.Session.Remove(ImportErrorsSessionKey);
                }
            }
            catch (Exception ex)
            {
                TempData["Errore"] = $"Si è verificato un errore critico durante l'importazione dei prodotti: {ex.Message}";

                HttpContext.Session.Remove(ExcelDataSessionKey);
                HttpContext.Session.Remove(FileNameSessionKey);
                HttpContext.Session.Remove(ImportErrorsSessionKey);
                TempData.Remove(MappingTempDataKey);
                return RedirectToAction("Index");
            }

            // Prepara i dati per la view di riepilogo dell'importazione
            ViewBag.ImportedProducts = importedProducts; // Passa la lista dei prodotti importati
            ViewBag.ErrorCount = importErrorsLog.Count;  // Passa il conteggio degli errori per la visualizzazione condizionale
            return View("RisultatoImportazioneProdotti");
        }

        // GET: ImportazioneProdotti/ImportazioneErrori
        // Mostra i dettagli degli errori riscontrati durante l'ultima importazione di prodotti.
        [HttpGet]
        public IActionResult ImportazioneErrori()
        {
            var errors = new List<string>();
            // Recupera gli errori serializzati JSON dalla sessione
            var errorsJson = HttpContext.Session.GetString(ImportErrorsSessionKey);
            if (!string.IsNullOrEmpty(errorsJson))
            {
                errors = JsonSerializer.Deserialize<List<string>>(errorsJson);
            }
            ViewBag.ImportErrors = errors;
            return View("ImportazioneErroriProdotti");
        }
    }
}