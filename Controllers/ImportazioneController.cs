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
        private readonly string _logDirectory;
        private const string ExcelDataSessionKey = "ExcelData";
        private const string FileNameSessionKey = "FileName";
        private const string MappingTempDataKey = "Mapping";
        private const string ImportErrorsSessionKey = "ImportErrors";

        public ImportazioneController(ApplicationDbContext dbContext, IWebHostEnvironment env)
        {
            _dbContext = dbContext;
            // Imposta il percorso della directory dei log per l'importazione dei clienti
            _logDirectory = Path.Combine(env.ContentRootPath, "Logs", "ImportazioneClienti");
            // Crea la directory dei log se non esiste
            Directory.CreateDirectory(_logDirectory);
        }

        // GET: Importazione/Index
        // Mostra la pagina di upload iniziale per i file Excel dei clienti.
        [HttpGet]
        public IActionResult Index()
        {
            // Pulisce eventuali dati di sessione relativi a importazioni precedenti all'inizio di una nuova.
            HttpContext.Session.Remove(ExcelDataSessionKey);
            HttpContext.Session.Remove(FileNameSessionKey);
            HttpContext.Session.Remove(ImportErrorsSessionKey); // Pulisci eventuali errori precedenti dalla sessione
            return View("UploadExcel");
        }

        // POST: Importazione/Mapping
        // Gestisce l'upload del file Excel, legge le intestazioni e reindirizza alla pagina di mapping.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Mapping(IFormFile file)
        {
            // Controlla se il file è nullo o vuoto.
            if (file == null || file.Length == 0)
            {
                TempData["Errore"] = "Seleziona un file Excel valido per l'importazione dei clienti.";
                return RedirectToAction("Index");
            }

            //  file in formato .xlsx
            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Errore"] = "Il file deve essere in formato .xlsx.";
                return RedirectToAction("Index");
            }

            try
            {
                // 3. Copia del file in memoria
                // Crea un flusso di memoria (MemoryStream) per lavorare con il file senza salvarlo su disco.
                using var stream = new MemoryStream();
                // Copia il contenuto del file caricato (IFormFile) nel MemoryStream.
                file.CopyTo(stream);
                // Riposiziona il puntatore dello stream all'inizio (posizione 0) per consentire la lettura da ClosedXML.
                stream.Position = 0;

                // 4. Apertura del workbook (cartella di lavoro Excel)
                using (var workbook = new XLWorkbook(stream))
                {
                    // 5. Ricerca del primo foglio di lavoro con dati
                    // inizialmetne null, verrà assegnato al primo foglio valido trovato.
                    IXLWorksheet worksheetToProcess = null;

                    // Itera su tutti i fogli presenti nel workbook
                    foreach (var ws in workbook.Worksheets)
                    {
                        // Controlla se il foglio corrente (ws) contiene delle celle utilizzate 
                        if (ws.CellsUsed().Any())
                        {
                            worksheetToProcess = ws; // Se trovato, assegna il foglio ed esce dal ciclo.
                            break;
                        }
                    }

                    // 6. Gestione del caso in cui nessun foglio contenga dati
                    // Se, dopo aver controllato tutti i fogli, 'worksheetToProcess' è ancora null,
                    // significa che il file Excel contiene solo fogli vuoti.
                    if (worksheetToProcess == null)
                    {
                        TempData["Errore"] = "Il file Excel non contiene fogli di lavoro con dati validi.";
                        return RedirectToAction("Index");
                    }

                    // 7. Lettura delle intestazioni dal foglio selezionato
                    // Ottiene le celle utilizzate nella prima riga del foglio processato.
                    // Filtra le celle che hanno valori nulli o solo spazi bianchi.
                    // Seleziona il valore di ogni cella, lo converte in stringa, rimuove spazi extra.
                    // Converte il risultato in una lista di stringhe.
                    var intestazioniExcel = worksheetToProcess.Row(1).CellsUsed()
                                                                  .Where(c => !string.IsNullOrWhiteSpace(c.Value.ToString()))
                                                                  .Select(c => c.Value.ToString().Trim())
                                                                  .ToList();

                    // 8. Gestione del caso in cui non vengano trovate intestazioni valide
                    // Se la lista delle intestazioni è vuota, significa che la prima riga del foglio valido non contiene dati utili.
                    if (!intestazioniExcel.Any())
                    {
                        TempData["Errore"] = $"Il foglio '{worksheetToProcess.Name}' non contiene intestazioni valide nella prima riga.";
                        return RedirectToAction("Index");
                    }

                    // 9. Preparazione dei dati per la vista di mapping
                    // Passa le intestazioni delle colonne Excel alla vista tramite ViewBag.
                    ViewBag.ColonneExcel = intestazioniExcel;
                    // 10. Salvataggio dei dati del file Excel nella sessione
                    // Salva l'intero contenuto del file Excel (come array di byte) nella sessione HTTP.
                    // Questo permette di mantenere i dati in memoria tra le richieste, senza doverli risalvare su disco.
                    HttpContext.Session.Set(ExcelDataSessionKey, stream.ToArray());
                    // Salva anche il nome originale del file.
                    HttpContext.Session.SetString(FileNameSessionKey, file.FileName);
                    // Salva il nome del foglio che è stato identificato come quello da processare.
                    // Questo è utile per l'azione di importazione successiva, per puntare direttamente al foglio giusto.
                    HttpContext.Session.SetString("MappedWorksheetName", worksheetToProcess.Name);
                    // Pulisce eventuali errori di importazione da sessioni precedenti.
                    HttpContext.Session.Remove(ImportErrorsSessionKey);

                    return View("Mapping");
                }
            }
            catch (Exception ex)
            {
                TempData["Errore"] = $"Si è verificato un errore durante la lettura del file Excel: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: Importazione/SalvaMapping
        // Riceve il mapping delle colonne dalla form, lo salva in TempData e reindirizza all'azione Importa.
        [HttpPost]
        [ValidateAntiForgeryToken] // Protezione CSRF
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

        // GET: Importazione/Importa
        // Esegue l'importazione dei dati dei clienti dal file Excel nel database,
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
                TempData["Errore"] = "Impossibile recuperare i dati per l'importazione. Riprova l'upload e il mapping del file clienti.";
                return RedirectToAction("Index");
            }

            // Deserializza il dizionario di mapping JSON
            var mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(mappingJson);

            var importedClients = new List<Cliente>(); // Lista per i clienti importati con successo
            var importErrorsLog = new List<string>();    // Lista per i messaggi di errore
            var processedEmailsInCurrentFile = new HashSet<string>(); // Per tenere traccia delle email già processate nel *file corrente*

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
                    var cliente = new Cliente(); // Crea una nuova istanza di Cliente per ogni riga
                    var currentRowErrors = new List<string>(); // Lista per gli errori specifici di questa riga
                    bool recordSuccessfullyParsed = true;

                    // righe vuote vengono ignorate
                    if (row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.Value.ToString())))
                    {
                        continue;
                    }

                    // Itera su tutti i mapping definiti dall'utente per popolare l'oggetto Cliente
                    foreach (var mappingEntry in mappings)
                    {
                        var excelColumnName = mappingEntry.Key;     // Nome della colonna Excel
                        var modelPropertyName = mappingEntry.Value; // Nome della proprietà nel modello Cliente

                        // Se l'utente non ha mappato un campo specifico, lo saltiamo
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
                        var cellValue = cell.GetValue<string>()?.Trim();

                        var property = typeof(Cliente).GetProperty(modelPropertyName);
                        if (property == null)
                        {
                            currentRowErrors.Add($"Proprietà '{modelPropertyName}' non trovata nel modello Cliente.");
                            recordSuccessfullyParsed = false;
                            continue;
                        }

                        try
                        {
                            // Gestisce i casi in cui il valore della cella è nullo o vuoto
                            if (string.IsNullOrWhiteSpace(cellValue))
                            {
                                if (Nullable.GetUnderlyingType(property.PropertyType) != null || property.PropertyType == typeof(string))
                                {
                                    property.SetValue(cliente, null);
                                }
                                else if (property.PropertyType.IsValueType)
                                {
                                    currentRowErrors.Add($"Valore obbligatorio mancante per il campo '{modelPropertyName}' (riga {row.RowNumber()}).");
                                    recordSuccessfullyParsed = false;
                                }
                                continue;
                            }

                            // Logica di conversione specifica per ogni tipo di dato del modello Cliente
                            if (property.PropertyType == typeof(string))
                            {
                                if ((modelPropertyName == "Nome" || modelPropertyName == "Cognome") &&
                                            double.TryParse(cellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                                {
                                    currentRowErrors.Add($"Valore '{cellValue}' non valido per il campo '{modelPropertyName}' (non può essere un numero).");
                                    recordSuccessfullyParsed = false;
                                }
                                else
                                {
                                    property.SetValue(cliente, cellValue);
                                }
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
                                // ClosedXML può restituire DateTime direttamente se il formato della cella è riconosciuto
                                if (cell.DataType == XLDataType.DateTime)
                                {
                                    property.SetValue(cliente, cell.GetDateTime());
                                }
                                // Tenta di parsare la data in diversi formati comuni se ClosedXML non la riconosce
                                else if (DateTime.TryParse(cellValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateValue))
                                {
                                    property.SetValue(cliente, dateValue);
                                }
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
                        }
                        catch (Exception ex)
                        {
                            currentRowErrors.Add($"Errore di conversione per '{modelPropertyName}' con valore '{cellValue}': {ex.Message}");
                            recordSuccessfullyParsed = false;
                        }
                    }

                    // Dopo aver processato tutti i campi, controlla se l'email è stata fornita e se è valida
                    if (recordSuccessfullyParsed && !string.IsNullOrEmpty(cliente.Email))
                    {
                        // Controllo email duplicata *nel database*
                        var existingClientInDb = await _dbContext.Clienti.FirstOrDefaultAsync(c => c.Email == cliente.Email);
                        if (existingClientInDb != null)
                        {
                            recordSuccessfullyParsed = false;
                            currentRowErrors.Add($"Email '{cliente.Email}' già presente nel database.");
                        }

                        // email duplicata *nel file di importazione corrente*
                        if (!recordSuccessfullyParsed && processedEmailsInCurrentFile.Contains(cliente.Email))
                        {
                            if (recordSuccessfullyParsed) //
                            {
                                currentRowErrors.Add($"Email '{cliente.Email}' duplicata nel file di importazione. Sarà importata solo la prima occorrenza.");
                                recordSuccessfullyParsed = false;
                            }
                        }
                        else if (recordSuccessfullyParsed) // Se il record è stato processato con successo e non ci sono stati errori
                        {
                            processedEmailsInCurrentFile.Add(cliente.Email);
                        }
                    }

                    // Se il record è stato processato con successo E non ci sono stati errori (parsing o logica di business)
                    if (recordSuccessfullyParsed && !currentRowErrors.Any())
                    {
                        importedClients.Add(cliente);
                    }
                    else // Se ci sono stati errori 
                    {
                        string rowContent = string.Join(", ", row.CellsUsed().Select(c => $"'{c.Value.ToString() ?? ""}'"));
                        importErrorsLog.Add($"Riga {row.RowNumber()} del file '{fileName}': Contenuto [{rowContent}] - Errori: {string.Join("; ", currentRowErrors)}");
                    }
                }

                // Salva tutti i clienti validi nel database in un'unica operazione batch
                // Questo è più efficiente che salvare un cliente alla volta.
                if (importedClients.Any())
                {
                    _dbContext.Clienti.AddRange(importedClients);
                    await _dbContext.SaveChangesAsync();
                }

                // Pulisci i dati di sessione usati per l'importazione una volta completata
                HttpContext.Session.Remove(ExcelDataSessionKey);
                HttpContext.Session.Remove(FileNameSessionKey);
                TempData.Remove(MappingTempDataKey);

                // Salva gli errori nel file di log e nella sessione per la visualizzazione temporanea
                if (importErrorsLog.Any())
                {
                    LogErrorsToFile(importErrorsLog, fileName);
                    HttpContext.Session.SetString(ImportErrorsSessionKey, JsonSerializer.Serialize(importErrorsLog));
                }
                else
                {
                    HttpContext.Session.Remove(ImportErrorsSessionKey);
                }
            }
            catch (Exception ex)
            {
                TempData["Errore"] = $"Si è verificato un errore critico durante l'importazione: {ex.Message}";
                // Log dell'errore critico
                LogErrorsToFile(new List<string> { $"Errore critico durante l'importazione del file '{fileName}': {ex.Message}" }, fileName, isCritical: true);

                // In caso di errore critico, pulisci comunque la sessione per evitare dati orfani o stato inconsistente
                HttpContext.Session.Remove(ExcelDataSessionKey);
                HttpContext.Session.Remove(FileNameSessionKey);
                HttpContext.Session.Remove(ImportErrorsSessionKey);
                TempData.Remove(MappingTempDataKey);
                return RedirectToAction("Index");
            }

            // Mostra sempre la pagina di riepilogo "Importa" 
            ViewBag.ImportedClients = importedClients;
            // Passa il conteggio degli errori alla view per decidere se mostrare il link
            ViewBag.ErrorCount = importErrorsLog.Count;
            return View("Importa");
        }

        // GET: Importazione/ImportazioneErrori
        // Mostra i dettagli degli errori riscontrati durante l'ultima importazione di clienti (dalla sessione).
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
            return View();
        }

        /// <summary>
        /// Scrive i messaggi di errore su un file di log nella directory configurata.
        /// </summary>
        /// <param name="errors">La lista dei messaggi di errore da loggare.</param>
        /// <param name="originalFileName">Il nome originale del file Excel da cui provengono gli errori.</param>
        /// <param name="isCritical">Indica se si tratta di un errore critico che interrompe l'intero processo.</param>
        private void LogErrorsToFile(List<string> errors, string originalFileName, bool isCritical = false)
        {
            if (!errors.Any())
            {
                return;
            }

            // Crea un nome di file di log unico per ogni importazione
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logFileName = Path.GetFileNameWithoutExtension(originalFileName) + $"_ImportErrors_{timestamp}.log";
            string logFilePath = Path.Combine(_logDirectory, logFileName);

            using (StreamWriter sw = new StreamWriter(logFilePath, true)) // 'true' per appendere al file se esiste
            {
                sw.WriteLine($"--- Log di Importazione Clienti: {DateTime.Now} ---");
                sw.WriteLine($"File processato: {originalFileName}");
                if (isCritical)
                {
                    sw.WriteLine("ERRORE CRITICO: L'importazione è stata interrotta a causa di un errore grave.");
                }
                foreach (var error in errors)
                {
                    sw.WriteLine($"- {error}");
                }
                sw.WriteLine("-------------------------------------------\n");
            }
        }
    }
}