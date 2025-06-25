using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer; // Per il debug, puoi rimuoverlo in produzione
using QuestPDF.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; 
using WebAppEF.Entities; 
using SkiaSharp;
using System.Threading.Tasks; 

namespace ProgettoStage.Repositories
{
    public class GeneratorePdfService
    {
        private readonly string _logoPath;
        private readonly string _ragioneSocialeAziendale;
        private readonly string _nomeAzienda;

        public GeneratorePdfService(string logoPath, string ragioneSocialeAziendale, string nomeAzienda)
        {
            _logoPath = logoPath;
            _ragioneSocialeAziendale = ragioneSocialeAziendale;
            _nomeAzienda = nomeAzienda;
        }

        private void ApplyStandardLayout(IDocumentContainer container, Action<IContainer> contentBuilder)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Height(50) // Altezza fissa per l'header
                    .Row(row =>
                    {
                        // Colonna per l'immagine del logo
                        row.ConstantColumn(100).AlignLeft().Element(container =>
                        {
                            if (!string.IsNullOrEmpty(_logoPath) && File.Exists(_logoPath))
                            {
                                // FIX: Utilizza .FitArea() per ridimensionare l'immagine all'interno dello spazio disponibile.
                                // Questo assicura che l'immagine si adatti ai 100 punti di larghezza della colonna
                                // senza causare overflow e mantenendo le proporzioni.
                                container.Image(_logoPath).FitArea(); 
                            }
                            else
                            {
                                container.AlignMiddle().AlignCenter().Text("Logo Placeholder").FontSize(8);
                            }
                        });

                        // Colonna per il nome dell'azienda
                        row.RelativeColumn().AlignCenter().Text(_nomeAzienda).SemiBold().FontSize(16);
                    });

                page.Content()
                    .Row(row =>
                    {
                        // Linea verticale a sinistra del contenuto
                        row.ConstantColumn(2).ExtendVertical().Canvas((canvas, size) =>
                        {
                            var paint = new SKPaint
                            {
                                Color = SKColors.Black,
                                StrokeWidth = 1
                            };
                            canvas.DrawLine(0, 0, 0, size.Height, paint);
                        });

                        // Contenuto principale del documento
                        row.RelativeColumn().PaddingLeft(10).Element(contentBuilder);
                    });

                page.Footer()
                    .Height(30)
                    .Column(column =>
                    {
                        column.Item().AlignCenter().Text(x =>
                        {
                            x.CurrentPageNumber();
                            x.Span(" di ");
                            x.TotalPages();
                        });
                        column.Item().AlignCenter().Text(_ragioneSocialeAziendale).FontSize(8);
                    });
            });
        }

        public void MostraAnteprimaLayoutStandard()
        {
            var document = Document.Create(container =>
            {
                ApplyStandardLayout(container, content =>
                {
                    content.Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Text("Anteprima del Layout Standard").FontSize(20).SemiBold().FontColor(Colors.Red.Darken2);
                        column.Item().Text("Questo è un testo di esempio per mostrare come appare il corpo del documento.").FontSize(12);

                        column.Item().Element(containerPara =>
                        {
                            containerPara
                                .PaddingVertical(5)
                                .Column(paragraph =>
                                {
                                    paragraph.Spacing(3);
                                    paragraph.Item().Text("Qui puoi vedere come si allinea il testo rispetto alla linea laterale.");
                                    paragraph.Item().Text("Il padding sinistro assicura che il contenuto non si sovrapponga.");
                                    paragraph.Item().Text("Puoi aggiungere paragrafi, elenchi, immagini e tabelle.");
                                });
                        });

                        column.Item().PaddingVertical(10).LineHorizontal(1);

                        column.Item().Text("Sezione di Esempio - Tabella").FontSize(14).SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Colonna 1").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Colonna 2").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Colonna 3").SemiBold();
                            });

                            table.Cell().PaddingVertical(2).Text("Dato 1-1");
                            table.Cell().PaddingVertical(2).Text("Dato 1-2");
                            table.Cell().PaddingVertical(2).AlignRight().Text("100.00 €");

                            table.Cell().PaddingVertical(2).Text("Dato 2-1");
                            table.Cell().PaddingVertical(2).Text("Dato 2-2");
                            table.Cell().PaddingVertical(2).AlignRight().Text("250.50 €");
                        });

                        column.Item().PaddingVertical(10).LineHorizontal(1);

                        column.Item().Text("Ulteriore Contenuto di Test.").FontSize(10).Italic();
                        column.Item().Text("Questa area si estenderà con il tuo contenuto reale.").FontSize(10).Italic();
                    });
                });
            });

            document.ShowInPreviewer();
        }

        public async Task<byte[]> GeneraRicevutaOrdinePdfAsync(Ordine ordine, IEnumerable<DettagliOrdine> dettagliOrdini)
        {
            if (ordine == null)
                throw new ArgumentNullException(nameof(ordine), "L'oggetto ordine non può essere nullo.");

            var document = Document.Create(container =>
            {
                ApplyStandardLayout(container, content =>
                {
                    content.Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Text($"Ricevuta Ordine #{ordine.IdOrdine}").FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                        column.Item().Text($"Data Ordine: {ordine.DataOrdine:dd/MM/yyyy HH:mm}");

                        if (ordine.Cliente != null)
                        {
                            column.Item().Text($"Cliente: {ordine.Cliente.Nome} {ordine.Cliente.Cognome}");
                            column.Item().Text($"Email: {ordine.Cliente.Email}");

                            var addressParts = new List<string>();
                            if (!string.IsNullOrWhiteSpace(ordine.Cliente.Indirizzo)) addressParts.Add(ordine.Cliente.Indirizzo);
                            if (!string.IsNullOrWhiteSpace(ordine.Cliente.Civico)) addressParts.Add(ordine.Cliente.Civico);
                            if (!string.IsNullOrWhiteSpace(ordine.Cliente.Citta)) addressParts.Add(ordine.Cliente.Citta);
                            if (!string.IsNullOrWhiteSpace(ordine.Cliente.Cap)) addressParts.Add(ordine.Cliente.Cap);
                            if (!string.IsNullOrWhiteSpace(ordine.Cliente.Paese)) addressParts.Add(ordine.Cliente.Paese);

                            var indirizzoCompleto = string.Join(", ", addressParts);
                            if (string.IsNullOrWhiteSpace(indirizzoCompleto))
                            {
                                indirizzoCompleto = "Nessun indirizzo specificato";
                            }
                            column.Item().Text($"Indirizzo: {indirizzoCompleto}");
                        }
                        else
                        {
                            column.Item().Text($"Cliente: Non disponibile (ID: {ordine.IdCliente})").Italic();
                        }

                        column.Item().Text($"Stato Ordine: {ordine.Stato}").SemiBold();

                        column.Item().PaddingVertical(10).LineHorizontal(1);

                        column.Item().Text("Dettagli Ordine").SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Descrizione
                                columns.RelativeColumn(1); // Quantità
                                columns.RelativeColumn(1); // Prezzo Unitario
                                columns.RelativeColumn(1); // Totale
                            });
                            // Aggiunta dell'Header alla tabella dei dettagli ordine
                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Prodotto").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Quantità").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Prezzo Unit.").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Totale").SemiBold();
                            });

                            if (dettagliOrdini != null && dettagliOrdini.Any())
                            {
                                foreach (var dettaglio in dettagliOrdini)
                                {
                                    table.Cell().PaddingVertical(2).Text(dettaglio.Prodotto?.NomeProdotto ?? "Prodotto non disponibile");
                                    table.Cell().PaddingVertical(2).AlignRight().Text(dettaglio.Quantita.ToString());
                                    table.Cell().PaddingVertical(2).AlignRight().Text($"{dettaglio.PrezzoUnitario:C}");
                                    table.Cell().PaddingVertical(2).AlignRight().Text($"{(dettaglio.Quantita * dettaglio.PrezzoUnitario):C}");
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(4).PaddingVertical(10).AlignCenter().Text("Nessun dettaglio ordine disponibile.");
                            }
                        });

                        column.Item().PaddingVertical(10).LineHorizontal(1);

                        column.Item().Text("Riepilogo Ordine").SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Descrizione
                                columns.RelativeColumn(1); // Valore
                            });

                            table.Cell().PaddingVertical(2).Text("Totale Ordine:").SemiBold();
                            table.Cell().AlignRight().Text($"{ordine.TotaleOrdine:C}").SemiBold();
                        });

                        column.Item().PaddingVertical(10).LineHorizontal(1);

                        column.Item().AlignCenter().Text("Grazie per il tuo ordine!").Italic().FontSize(12);
                    });
                });
            });

            return await Task.Run(() => document.GeneratePdf());
        }

        /// <summary>
        /// Genera l'anagrafica dei clienti in formato PDF.
        /// </summary>
        /// <param name="clienti">Una collezione di oggetti Cliente.</param>
        /// <returns>Un array di byte contenente il PDF generato.</returns>
        public async Task<byte[]> GeneraAnagraficaClientiPdfAsync(IEnumerable<Cliente> clienti)
        {
            if (clienti == null || !clienti.Any())
            {
                throw new ArgumentNullException(nameof(clienti), "L'elenco dei clienti non può essere nullo o vuoto.");
            }

            var document = Document.Create(container =>
            {
                ApplyStandardLayout(container, content =>
                {
                    content.Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Text("Anagrafica Clienti").FontSize(16).SemiBold().FontColor(Colors.Green.Darken2);

                        column.Item().PaddingVertical(10).LineHorizontal(1);

                        column.Item().Text("Dettagli Clienti").SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1); // ID
                                columns.RelativeColumn(2); // Nome
                                columns.RelativeColumn(2); // Cognome
                                columns.RelativeColumn(3); // Email
                                columns.RelativeColumn(2); // Data Iscrizione
                                columns.RelativeColumn(1); // Attivo
                                columns.RelativeColumn(3); // Indirizzo Completo
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("ID").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Nome").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Cognome").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Email").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).AlignCenter().Text("Data Iscrizione").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).AlignCenter().Text("Attivo").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Indirizzo Completo").SemiBold();
                            });

                            foreach (var cliente in clienti)
                            {
                                table.Cell().PaddingVertical(2).Text(cliente.IdCliente.ToString());
                                table.Cell().PaddingVertical(2).Text(cliente.Nome ?? "N/A");
                                table.Cell().PaddingVertical(2).Text(cliente.Cognome ?? "N/A");
                                table.Cell().PaddingVertical(2).Text(cliente.Email ?? "N/A");
                                table.Cell().PaddingVertical(2).AlignCenter().Text(cliente.DataIscrizione.ToString("dd/MM/yyyy"));
                                table.Cell().PaddingVertical(2).AlignCenter().Text(cliente.Attivo ? "Sì" : "No");

                                var addressParts = new List<string>();
                                if (!string.IsNullOrWhiteSpace(cliente.Indirizzo)) addressParts.Add(cliente.Indirizzo);
                                if (!string.IsNullOrWhiteSpace(cliente.Civico)) addressParts.Add(cliente.Civico);
                                if (!string.IsNullOrWhiteSpace(cliente.Citta)) addressParts.Add(cliente.Citta);
                                if (!string.IsNullOrWhiteSpace(cliente.Cap)) addressParts.Add(cliente.Cap);
                                if (!string.IsNullOrWhiteSpace(cliente.Paese)) addressParts.Add(cliente.Paese);

                                var indirizzoCompleto = string.Join(", ", addressParts);
                                if (string.IsNullOrWhiteSpace(indirizzoCompleto))
                                {
                                    indirizzoCompleto = "Nessun indirizzo specificato";
                                }
                                table.Cell().PaddingVertical(2).Text(indirizzoCompleto);
                            }
                        });

                        column.Item().PaddingVertical(10).LineHorizontal(1);
                        column.Item().AlignCenter().Text("Fine dell'Anagrafica Clienti.").FontSize(10).Italic();
                    });
                });
            });

            return await Task.Run(() => document.GeneratePdf());
        }

        /// <summary>
        /// Genera l'anagrafica dei prodotti in formato PDF.
        /// </summary>
        /// <param name="prodotti">Una collezione di oggetti Prodotto.</param>
        /// <returns>Un array di byte contenente il PDF generato.</returns>
        public async Task<byte[]> GeneraAnagraficaProdottiPdfAsync(IEnumerable<Prodotto> prodotti)
        {
            if (prodotti == null || !prodotti.Any())
            {
                throw new ArgumentNullException(nameof(prodotti), "L'elenco dei prodotti non può essere nullo o vuoto.");
            }

            var document = Document.Create(container =>
            {
                ApplyStandardLayout(container, content =>
                {
                    content.Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Text("Anagrafica Prodotti").FontSize(16).SemiBold().FontColor(Colors.Orange.Darken2);

                        column.Item().PaddingVertical(10).LineHorizontal(1);

                        column.Item().Text("Dettagli Prodotti").SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1); // ID Prodotto
                                columns.RelativeColumn(2); // Nome Prodotto
                                columns.RelativeColumn(1); // Categoria
                                columns.RelativeColumn(1); // Prezzo
                                columns.RelativeColumn(1); // Giacenza
                                columns.RelativeColumn(2); // Data Inserimento
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("ID Prodotto").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Nome").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Categoria").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Prezzo").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).AlignCenter().Text("Giacenza").SemiBold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Data Inserimento").SemiBold();
                            });

                            foreach (var prodotto in prodotti)
                            {
                                table.Cell().PaddingVertical(2).Text(prodotto.IdProdotto.ToString());
                                table.Cell().PaddingVertical(2).Text(prodotto.NomeProdotto ?? "N/A");
                                table.Cell().PaddingVertical(2).Text(prodotto.Categoria ?? "N/A");
                                table.Cell().PaddingVertical(2).AlignRight().Text($"{prodotto.Prezzo:C}");
                                table.Cell().PaddingVertical(2).AlignCenter().Text(prodotto.Giacenza.ToString());
                                table.Cell().PaddingVertical(2).Text(prodotto.DataInserimento.ToString("dd/MM/yyyy"));
                            }
                        });

                        column.Item().PaddingVertical(10).LineHorizontal(1);
                        column.Item().AlignCenter().Text("Fine dell'Anagrafica Prodotti.").FontSize(10).Italic();
                    });
                });
            });

            return await Task.Run(() => document.GeneratePdf());
        }
    }
}