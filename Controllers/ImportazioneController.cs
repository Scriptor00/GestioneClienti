using System.Globalization; 
using System.Text.Json;
using ClosedXML.Excel; 
using Microsoft.AspNetCore.Mvc;
using WebAppEF.Entities; 
using Microsoft.EntityFrameworkCore;
using WebAppEF.Models; 

namespace ProgettoStage.Controllers
{
    public class ImportazioneController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private const string ExcelDataSessionKey = "ExcelData";
        private const string FileNameSessionKey = "FileName";
        private const string MappingTempDataKey = "Mapping";
        private const string ImportErrorsSessionKey = "ImportErrors"; 

        public ImportazioneController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET: Pagina di upload iniziale
        [HttpGet]
        public IActionResult Index()
        {
            // Pulisci eventuali dati di sessione relativi a importazioni precedenti all'inizio di una nuova
            HttpContext.Session.Remove(ExcelDataSessionKey);
            HttpContext.Session.Remove(FileNameSessionKey);
            HttpContext.Session.Remove(ImportErrorsSessionKey); // Pulisci eventuali errori precedenti
            return View("UploadExcel");
        }

        // POST: Gestisce l'upload del file e reindirizza alla pagina di mapping
        [HttpPost]
        public IActionResult Mapping(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Errore"] = "Seleziona un file Excel valido.";
                return RedirectToAction("Index");
            }

            // Validazione dell'estensione del file
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

                // Apri il workbook dal MemoryStream
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

                // Passa le intestazioni delle colonne Excel alla vista tramite ViewBag
                ViewBag.ColonneExcel = intestazioniExcel;

                // Salva l'array di byte del file Excel e il nome del file direttamente nella sessione.
                HttpContext.Session.Set(ExcelDataSessionKey, stream.ToArray());
                HttpContext.Session.SetString(FileNameSessionKey, file.FileName);
                HttpContext.Session.Remove(ImportErrorsSessionKey); // Pulisci eventuali errori della sessione precedente

                return View("Mapping");
            }
            catch (Exception ex)
            {
                TempData["Errore"] = $"Si è verificato un errore durante la lettura del file: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Salva il mapping e avvia l'importazione
        [HttpPost]
        public IActionResult SalvaMapping([FromForm] Dictionary<string, string> mappings)
        {
            // Verifica che siano stati selezionati dei mapping
            if (mappings == null || !mappings.Any())
            {
                TempData["Errore"] = "Nessun mapping selezionato.";
                return RedirectToAction("Index");
            }

            // Serializza il dizionario di mapping in JSON e salvalo in TempData.
            TempData[MappingTempDataKey] = JsonSerializer.Serialize(mappings);
            return RedirectToAction("Importa");
        }

        // GET: Esegue l'importazione e mostra i risultati/log
        [HttpGet]
        public async Task<IActionResult> Importa()
        {
            var mappingJson = TempData[MappingTempDataKey] as string;
            byte[] excelBytes = HttpContext.Session.Get(ExcelDataSessionKey);
            string fileName = HttpContext.Session.GetString(FileNameSessionKey);

            // Verifica che i dati necessari per l'importazione siano disponibili
            if (string.IsNullOrEmpty(mappingJson) || excelBytes == null || excelBytes.Length == 0)
            {
                TempData["Errore"] = "Impossibile recuperare i dati per l'importazione. Riprova l'upload e il mapping.";
                return RedirectToAction("Index");
            }

            // Deserializza il dizionario di mapping
            var mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(mappingJson);

            // Liste per i clienti importati con successo e per gli errori
            var importedClients = new List<Cliente>();
            var importErrorsLog = new List<string>();

            try
            {
                // Crea un MemoryStream dall'array di byte recuperato e apri il workbook
                using var stream = new MemoryStream(excelBytes);
                stream.Position = 0; // Riposiziona lo stream all'inizio per la lettura
                using var workbook = new XLWorkbook(stream);
                var ws = workbook.Worksheet(1); // Ottieni il primo foglio di lavoro

                var headerRow = ws.Row(1);
                var dataRows = ws.RowsUsed().Skip(1); // Salta la riga dell'intestazione per leggere solo i dati

                foreach (var row in dataRows)
                {
                    var cliente = new Cliente();
                    var currentRowErrors = new List<string>(); // Errori specifici per la riga corrente
                    bool recordSuccessfullyParsed = true;

                    // Ignora righe completamente vuote (controlla se tutte le celle sono vuote)
                    if (row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.Value.ToString())))
                    {
                        continue;
                    }

                    // Itera su tutti i mapping definiti dall'utente
                    foreach (var mappingEntry in mappings)
                    {
                        var excelColumnName = mappingEntry.Key;     // Nome della colonna in Excel (es. "Cognome Cliente")
                        var modelPropertyName = mappingEntry.Value; // Nome della proprietà nel modello Cliente (es. "Cognome")

                        // Se l'utente non ha mappato un campo, lo saltiamo
                        if (string.IsNullOrWhiteSpace(modelPropertyName))
                        {
                            continue;
                        }

                        // Trova la cella dell'intestazione corrispondente al nome della colonna Excel
                        var excelColumnCell = headerRow.CellsUsed().FirstOrDefault(c => c.Value.ToString().Trim().Equals(excelColumnName, StringComparison.OrdinalIgnoreCase));
                        if (excelColumnCell == null)
                        {
                            currentRowErrors.Add($"Colonna Excel '{excelColumnName}' non trovata per il mapping '{modelPropertyName}'.");
                            recordSuccessfullyParsed = false;
                            continue;
                        }

                        var excelColumnIndex = excelColumnCell.Address.ColumnNumber;
                        var cell = row.Cell(excelColumnIndex);
                        var cellValue = cell.GetValue<string>()?.Trim(); // Ottieni il valore come stringa e trimma, gestendo null

                        // Ottieni la proprietà del modello Cliente tramite Reflection
                        var property = typeof(Cliente).GetProperty(modelPropertyName);
                        if (property == null)
                        {
                            // Questo dovrebbe non accadere se colonneModel è corretto
                            currentRowErrors.Add($"Proprietà '{modelPropertyName}' non trovata nel modello Cliente.");
                            recordSuccessfullyParsed = false;
                            continue;
                        }

                        // Gestione della conversione dei tipi di dato
                        try
                        {
                            // Gestione di valori null/vuoti per tipi non stringa
                            if (string.IsNullOrWhiteSpace(cellValue))
                            {
                                // Se la proprietà è un tipo nullable (es. int?, DateTime?, bool?)
                                // o una stringa, possiamo impostare null.
                                if (Nullable.GetUnderlyingType(property.PropertyType) != null || property.PropertyType == typeof(string))
                                {
                                    property.SetValue(cliente, null);
                                }
                                // Se è un tipo non-nullable e non è stringa, è un errore di valore mancante
                                else if (property.PropertyType.IsValueType) // solo per value types, non per string
                                {
                                    currentRowErrors.Add($"Valore obbligatorio mancante per il campo '{modelPropertyName}' (riga {row.RowNumber()}).");
                                    recordSuccessfullyParsed = false;
                                }
                                continue; // Salta al prossimo mapping per questa riga
                            }

                            if (property.PropertyType == typeof(string))
                            {
                                property.SetValue(cliente, cellValue);
                            }
                            else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
                            {
                                if (int.TryParse(cellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out int intValue))
                                {
                                    property.SetValue(cliente, intValue);
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
                                    property.SetValue(cliente, decimalValue);
                                }
                                else
                                {
                                    currentRowErrors.Add($"Valore '{cellValue}' non valido per il campo '{modelPropertyName}' (atteso numero decimale).");
                                    recordSuccessfullyParsed = false;
                                }
                            }
                            else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                            {
                                // ClosedXML può restituire DateTime direttamente se il formato è riconosciuto
                                if (cell.DataType == XLDataType.DateTime)
                                {
                                    property.SetValue(cliente, cell.GetDateTime());
                                }
                                else if (DateTime.TryParse(cellValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateValue))
                                {
                                    property.SetValue(cliente, dateValue);
                                }
                                // Gestione di formati data alternativi (es. "gg/mm/aaaa", "aaaa-mm-gg")
                                else if (DateTime.TryParseExact(cellValue, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue))
                                {
                                    property.SetValue(cliente, dateValue);
                                }
                                else if (DateTime.TryParseExact(cellValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue))
                                {
                                    property.SetValue(cliente, dateValue);
                                }
                                else
                                {
                                    currentRowErrors.Add($"Valore '{cellValue}' non valido per il campo '{modelPropertyName}' (attesa data valida).");
                                    recordSuccessfullyParsed = false;
                                }
                            }
                            else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                            {
                                // Gestione di diverse rappresentazioni booleane (Sì/No, True/False, 1/0)
                                if (bool.TryParse(cellValue, out bool boolValue))
                                {
                                    property.SetValue(cliente, boolValue);
                                }
                                else if (cellValue.Equals("sì", StringComparison.OrdinalIgnoreCase) || cellValue.Equals("si", StringComparison.OrdinalIgnoreCase) || cellValue.Equals("true", StringComparison.OrdinalIgnoreCase) || cellValue == "1")
                                {
                                    property.SetValue(cliente, true);
                                }
                                else if (cellValue.Equals("no", StringComparison.OrdinalIgnoreCase) || cellValue.Equals("false", StringComparison.OrdinalIgnoreCase) || cellValue == "0")
                                {
                                    property.SetValue(cliente, false);
                                }
                                else
                                {
                                    currentRowErrors.Add($"Valore '{cellValue}' non valido per il campo '{modelPropertyName}' (atteso booleano: true/false, sì/no, 1/0).");
                                    recordSuccessfullyParsed = false;
                                }
                            }
                            // Aggiungi qui altre conversioni di tipo se hai proprietà diverse nel modello (es. Guid, enum)
                        }
                        catch (Exception ex)
                        {
                            currentRowErrors.Add($"Errore di conversione per '{modelPropertyName}' con valore '{cellValue}': {ex.Message}");
                            recordSuccessfullyParsed = false;
                        }
                    }

                   
                    if (recordSuccessfullyParsed && !string.IsNullOrEmpty(cliente.Email))
                    {
                        // Controlla se l'email esiste già nel database
                        var existingClient = await _dbContext.Clienti.FirstOrDefaultAsync(c => c.Email == cliente.Email);

                        if (existingClient != null)
                        {
                            recordSuccessfullyParsed = false;
                            currentRowErrors.Add($"Email '{cliente.Email}' già presente nel database.");
                        }
                    }
                    
                   // Se il record è stato processato con successo E non ha errori di logica di business
                    if (recordSuccessfullyParsed && !currentRowErrors.Any())
                    {
                        importedClients.Add(cliente);
                    }
                    else // Se ci sono stati errori di parsing o di logica di business (email duplicata)
                    {
                        // Costruisci il contenuto originale della riga per il log errori
                        string rowContent = string.Join(", ", row.CellsUsed().Select(c => $"'{c.Value.ToString() ?? ""}'"));
                        importErrorsLog.Add($"Riga {row.RowNumber()} del file '{fileName}': Contenuto [{rowContent}] - Errori: {string.Join("; ", currentRowErrors)}");
                    }
                }

                // Salva tutte le modifiche al database in un'unica operazione solo dopo aver processato tutte le righe
                if (importedClients.Any())
                {
                    _dbContext.Clienti.AddRange(importedClients);
                    await _dbContext.SaveChangesAsync();
                }

                HttpContext.Session.Remove(ExcelDataSessionKey);
                HttpContext.Session.Remove(FileNameSessionKey);
                TempData.Remove(MappingTempDataKey); // TempData viene rimosso automaticamente dopo la lettura, ma per chiarezza lo mettiamo qui

                // Se ci sono errori, salvali in sessione per la pagina separata di visualizzazione errori
                if (importErrorsLog.Any())
                {
                    HttpContext.Session.SetString(ImportErrorsSessionKey, JsonSerializer.Serialize(importErrorsLog));
                }
                else
                {
                    HttpContext.Session.Remove(ImportErrorsSessionKey);
                }
            }
            catch (Exception ex)
            {
                // Gestione di errori critici durante l'elaborazione del file o il salvataggio nel DB
                TempData["Errore"] = $"Si è verificato un errore critico durante l'importazione: {ex.Message}";

                HttpContext.Session.Remove(ExcelDataSessionKey);
                HttpContext.Session.Remove(FileNameSessionKey);
                HttpContext.Session.Remove(ImportErrorsSessionKey);
                TempData.Remove(MappingTempDataKey);
                return RedirectToAction("Index");
            }

            // Mostra sempre la pagina di riepilogo "RisultatoImportazione"
            ViewBag.ImportedClients = importedClients;
            // Passa il conteggio degli errori alla view per decidere se mostrare il link
            ViewBag.ErrorCount = importErrorsLog.Count;
            return View("Importa"); 
        }

        [HttpGet]
        public IActionResult ImportazioneErrori()
        {
            var errors = new List<string>();
            // Recupera gli errori dalla sessione
            var errorsJson = HttpContext.Session.GetString(ImportErrorsSessionKey);
            if (!string.IsNullOrEmpty(errorsJson))
            {
                errors = JsonSerializer.Deserialize<List<string>>(errorsJson);
            }
            ViewBag.ImportErrors = errors;
            // Opzionalmente, puoi decidere di pulire gli errori dalla sessione dopo averli visualizzati
            // HttpContext.Session.Remove(ImportErrorsSessionKey);
            return View(); // Restituisce la view ImportazioneErrori.cshtml
        }
    }
}