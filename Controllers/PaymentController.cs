using GestioneClienti.Repositories;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace GestioneClienti.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AutoValidateAntiforgeryToken]
    public class PaymentController : ControllerBase
    {
        private readonly string _stripeSecretKey;
        private readonly ILogger<PaymentController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly EmailSettings _emailSettings; 

        public PaymentController(IConfiguration configuration, ILogger<PaymentController> logger, IEmailSender emailSender, IOptions<EmailSettings> emailSettings)
        {
            _stripeSecretKey = configuration["Stripe:SecretKey"];
            _logger = logger;
            _emailSender = emailSender;
            _emailSettings = emailSettings.Value;
        }

        [HttpPost("process-payment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            _logger.LogInformation("Ricevuta richiesta POST a /api/Payment/process-payment");

            if (request == null)
            {
                _logger.LogWarning("Richiesta di pagamento nulla.");
                return BadRequest(new { error = "Richiesta di pagamento non valida." });
            }

            _logger.LogDebug($"Dati della richiesta ricevuti: Token = {request.Token}, Amount = {request.Amount}, Currency = {request.Currency}");

            if (string.IsNullOrEmpty(request.Token))
            {
                _logger.LogWarning("Il token di pagamento è nullo o vuoto.");
                return BadRequest(new { error = "Richiesta di pagamento non valida: token mancante." });
            }

            if (request.Amount <= 0)
            {
                _logger.LogWarning($"L'importo del pagamento ({request.Amount}) non è valido.");
                return BadRequest(new { error = "Richiesta di pagamento non valida: importo non valido." });
            }

            if (string.IsNullOrEmpty(request.Currency))
            {
                _logger.LogWarning("La valuta del pagamento è nulla o vuota.");
                return BadRequest(new { error = "Richiesta di pagamento non valida: valuta mancante." });
            }

            _logger.LogInformation("Richiesta di pagamento valida. Inizializzazione elaborazione con Stripe.");

            StripeConfiguration.ApiKey = _stripeSecretKey;

            var chargeOptions = new ChargeCreateOptions
            {
                Amount = request.Amount, //in centesimi
                Currency = request.Currency,
                Source = request.Token,
                Description = "Pagamento per ordine #" + Guid.NewGuid(),
            };

            var service = new ChargeService();

            _logger.LogDebug("Creazione del servizio di addebito Stripe.");

            try
            {
                _logger.LogInformation("Tentativo di creazione dell'addebito con Stripe.");
                var charge = await service.CreateAsync(chargeOptions);
                _logger.LogInformation($"Addebito Stripe creato con ID: {charge.Id}, Stato: {charge.Status}");

                if (charge.Status == "succeeded")
                {
                    _logger.LogInformation($"Pagamento riuscito per l'addebito ID: {charge.Id}.");

                    var userEmail = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;

                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        var subject = "🎮 Conferma Ordine - Grazie per il tuo acquisto!";
                        var message = new MimeMessage();
                        message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.FromEmail));
                        message.To.Add(new MailboxAddress("", userEmail));
                        message.Subject = subject;

                        var bodyBuilder = new BodyBuilder
                        {
                            HtmlBody = $@"
    <html>
        <body style=""font-family: 'Segoe UI', Arial, sans-serif; background-color: #0f0f12; color: #e0e0e0; margin: 0; padding: 20px;"">
            <div style=""max-width: 600px; margin: 0 auto; border: 1px solid #2a2a3a; border-radius: 8px; overflow: hidden; background-color: #1a1a2a;"">
                <div style=""background: linear-gradient(90deg, #6e48aa 0%, #9d50bb 100%); padding: 20px; text-align: center;"">
                    <h1 style=""margin: 0; color: white; font-size: 28px; font-weight: bold; text-shadow: 0 2px 4px rgba(0,0,0,0.3);"">🎮 Ordine Confermato! 🎮</h1>
                </div>

                <div style=""padding: 25px;"">
                    <p style=""font-size: 16px; line-height: 1.6;"">Grazie per il tuo acquisto nel nostro <strong style=""color: #9d50bb;"">gaming store</strong>! Il tuo supporto ci aiuta a portarti le ultime novità tech.</p>

                    <div style=""background-color: #25253a; padding: 15px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #6e48aa;"">
                        <p style=""margin: 5px 0;""><strong style=""color: #b8b8ff;"">ID Transazione:</strong> <span style=""font-family: monospace; color: #e0e0e0;"">{charge.Id}</span></p>
                    </div>

                    <p style=""font-size: 16px; line-height: 1.6;"">Il tuo ordine è in fase di elaborazione. Riceverai un'email con gli aggiornamenti sulla spedizione entro 24 ore.</p>

                    <p style=""font-size: 14px; color: #a0a0a0;"">Hai domande? Rispondi a questa email o contattaci su <a href=""https://discord.gg/tuo-link"" style=""color: #6e48aa; text-decoration: none;"">Discord</a>!</p>
                </div>

                <div style=""background-color: #0f0f1a; padding: 15px; text-align: center; font-size: 12px; color: #7a7a8c;"">
                    <p style=""margin: 0;"">© 2024 <strong style=""color: #9d50bb;"">[Gaming Store]</strong>. Tutti i diritti riservati.</p>
                    <p style=""margin: 10px 0 0;"">
                        <a href=""https://twitter.com/tuostore"" style=""color: #6e48aa; text-decoration: none; margin: 0 10px;"">Twitter</a>
                        <a href=""https://instagram.com/tuostore"" style=""color: #6e48aa; text-decoration: none; margin: 0 10px;"">Instagram</a>
                    </p>
                </div>
            </div>
        </body>
    </html>"
                        };
                        message.Body = bodyBuilder.ToMessageBody();

                        try
                        {
                            using (var client = new SmtpClient())
                            {
                                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.MailPort, SecureSocketOptions.StartTls);
                                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                                await client.SendAsync(message);
                                await client.DisconnectAsync(true);
                                _logger.LogInformation("Email di conferma ordine inviata con successo a {Email}", userEmail);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Errore durante l'invio dell'email di conferma ordine a {Email}", userEmail);
                            // Potresti voler gestire questo errore in modo appropriato (es. loggare, mostrare un messaggio all'utente)
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Impossibile recuperare l'email dell'utente per inviare la conferma dell'ordine.");
                        // Gestisci il caso in cui l'email non è disponibile
                    }

                    return Ok(new { success = true, chargeId = charge.Id });
                }
                else
                {
                    _logger.LogWarning($"Il pagamento non è andato a buon fine. Stato dell'addebito: {charge.Status}");
                    return BadRequest(new { error = "Il pagamento non è andato a buon fine.", chargeStatus = charge.Status });
                }
            }
            catch (StripeException e)
            {
                _logger.LogError($"Errore durante l'elaborazione del pagamento con Stripe: {e.Message}");
                return BadRequest(new { error = "Errore durante l'elaborazione del pagamento con Stripe.", message = e.Message });
            }
            catch (System.Exception e)
            {
                _logger.LogError($"Si è verificato un errore imprevisto durante l'elaborazione del pagamento: {e.Message}");
                return StatusCode(500, new { error = "Si è verificato un errore imprevisto.", message = e.Message });
            }
        }

        public class PaymentRequest
        {
            public string Token { get; set; }
            public int Amount { get; set; }
            public string Currency { get; set; }
        }
    }
}