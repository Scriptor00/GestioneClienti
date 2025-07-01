using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.IO; // Necessario per MemoryStream

namespace GestioneClienti.Repositories
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailSender> _logger;
        private readonly IConfiguration _configuration;

        public EmailSender(IOptions<EmailSettings> emailSettings, ILogger<EmailSender> logger, IConfiguration configuration)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _configuration = configuration;
        }

        // Questo è il metodo "generico" SendEmailAsync, ora delega al metodo con allegati
        // Se non ci sono allegati, vengono passati come null/vuoti.
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Delega al metodo con allegato, passando null per i parametri dell'allegato
            await SendEmailWithAttachmentAsync(email, subject, htmlMessage, null, null, null);
        }

        // NUOVO METODO PRINCIPALE per l'invio di email, che gestisce anche gli allegati
        public async Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, byte[]? attachmentBytes, string? attachmentFileName, string? attachmentContentType)
        {
            try
            {
                _logger.LogInformation("Preparazione invio email a {Email}", email);
                _logger.LogDebug("Configurazione SMTP: Server={SmtpServer}:{SmtpPort}, FromEmail={FromEmail}",
                    _emailSettings.SmtpServer, _emailSettings.MailPort, _emailSettings.FromEmail);

                using (var client = new SmtpClient())
                {
                    // Connessione all'SMTP
                    await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.MailPort, SecureSocketOptions.StartTls);
                    // Autenticazione (se richiesta dal server SMTP)
                    await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);

                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.FromEmail));
                    message.To.Add(new MailboxAddress("", email)); // Nessun nome per il destinatario, solo l'email
                    message.Subject = subject;

                    // Crea il BodyBuilder per il contenuto dell'email (HTML)
                    var bodyBuilder = new BodyBuilder
                    {
                        HtmlBody = htmlMessage
                    };

                    // Aggiungi l'allegato se i dati sono stati forniti
                    if (attachmentBytes != null && attachmentBytes.Length > 0 &&
                        !string.IsNullOrEmpty(attachmentFileName) && !string.IsNullOrEmpty(attachmentContentType))
                    {
                        _logger.LogInformation("Aggiunta allegato: {FileName} ({ContentType})", attachmentFileName, attachmentContentType);

                        var attachment = new MimePart(attachmentContentType) // Usa il ContentType fornito
                        {
                            Content = new MimeContent(new MemoryStream(attachmentBytes)),
                            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment), // Indica che è un allegato
                            ContentTransferEncoding = ContentEncoding.Base64, // Codifica base64 per il trasferimento
                            FileName = attachmentFileName // Nome del file che apparirà nell'email
                        };
                        bodyBuilder.Attachments.Add(attachment);
                    }

                    // Imposta il corpo del messaggio
                    message.Body = bodyBuilder.ToMessageBody();

                    // Invia l'email
                    _logger.LogInformation("Invio email a {Email} con oggetto '{Subject}'", email, subject);
                    await client.SendAsync(message);

                    // Disconnetti dal server SMTP
                    await client.DisconnectAsync(true);
                    _logger.LogInformation("Email inviata con successo a {Email}", email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'invio email a {Email} con oggetto '{Subject}'", email, subject);
                throw; // Rilancia l'eccezione per essere gestita dal chiamante
            }
        }

        // Metodo per l'email di benvenuto (riutilizza SendEmailAsync)
        public async Task SendWelcomeEmail(string email, string username)
        {
            var baseUrl = _configuration["BaseUrl"];
            string subject = "Benvenuto nella nostra piattaforma!";

            string htmlMessage = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Benvenuto, {username}!</title>
    <style>
        body {{
            background-color: #0f0f1a;
            color: #e0e0ff;
            font-family: 'Segoe UI', Arial, sans-serif;
            line-height: 1.6;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            padding: 20px;
            background-color: #1a1a2e;
            border-radius: 8px;
            border: 1px solid #2d2d5a;
        }}
        .header {{
            color: #6c5ce7;
            font-size: 24px;
            text-align: center;
            margin-bottom: 20px;
            text-shadow: 0 0 5px rgba(108, 92, 231, 0.3);
        }}
        .username {{
            color: #00cec9;
            font-weight: bold;
        }}
        .btn {{
            display: inline-block;
            padding: 12px 24px;
            margin: 20px 0;
            background: linear-gradient(135deg, #6c5ce7 0%, #00cec9 100%);
            color: white !important;
            text-decoration: none;
            border-radius: 4px;
            font-weight: bold;
            text-align: center;
        }}
        .footer {{
            margin-top: 30px;
            font-size: 14px;
            color: #a0a0c0;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>🎮 Benvenuto nella Nostra Gaming Community! 🎮</div>

        <p>Ciao <span class='username'>{username}</span>,</p>

        <p>Il tuo account è stato creato con successo! Preparati a vivere un'esperienza di gioco straordinaria.</p>

        <p>Ecco cosa puoi fare ora:</p>
        <ul>
            <li>Esplora il nostro catalogo di giochi</li>
            <li>Unisciti alla community e trova nuovi compagni di squadra</li>
            <li>Scopri le offerte esclusive per i nuovi membri</li>
        </ul>

        <center>
            <a href='{baseUrl}/Prodotti/Home' class='btn'>INIZIA L'AVVENTURA</a>
        </center>

        <p>Se hai bisogno di aiuto, il nostro team di supporto è sempre pronto ad assisterti.</p>

        <div class='footer'>
            <p>Ci vediamo in gioco!<br>
            <strong>Il Team di Gaming Store</strong></p>
            <p style='font-size:12px;margin-top:10px;'>© {DateTime.Now.Year} Gaming Store. Tutti i diritti riservati.</p>
        </div>
    </div>
</body>
</html>
";
            await SendEmailAsync(email, subject, htmlMessage);
        }

        // Metodo per l'email di conferma (riutilizza SendEmailAsync)
        public async Task SendEmailConferma(string email, string username, string token)
        {
            var baseUrl = _configuration["BaseUrl"];
            var confermaUrl = $"{baseUrl}/Account/ConfermaEmail?token={token}";
            var subject = "Conferma la tua registrazione";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            line-height: 1.6;
            margin: 0;
            padding: 20px;
            background-color: #f9f9f9;
            color: #333333;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            padding: 30px;
            border-radius: 8px;
            border: 1px solid #e0e0e0;
        }}
        .header {{
            color: #6c5ce7;
            font-size: 24px;
            text-align: center;
            margin-bottom: 20px;
            text-shadow: 0 0 2px rgba(108, 92, 231, 0.3);
        }}
        .username {{
            color: #00cec9;
            font-weight: bold;
        }}
        .btn-confirm {{
            display: inline-block;
            padding: 12px 24px;
            margin: 20px 0;
            background: linear-gradient(135deg, #6c5ce7 0%, #00cec9 100%);
            color: black !important; /* Testo nero */
            text-decoration: none;
            border-radius: 4px;
            font-weight: bold;
            text-align: center;
        }}
        .footer {{
            margin-top: 30px;
            font-size: 14px;
            color: #777777;
            border-top: 1px solid #e0e0e0;
            padding-top: 20px;
            text-align: center;
        }}

        @media (prefers-color-scheme: dark) {{
            body {{
                background-color: #121212;
                color: #e0e0e0;
            }}
            .container {{
                background-color: #1e1e1e;
                border: 1px solid #333333;
                box-shadow: 0 0 10px rgba(0, 0, 0, 0.5);
            }}
            .header {{
                color: #a78bfa;
                text-shadow: 0 0 5px rgba(167, 139, 250, 0.5);
            }}
            .username {{
                color: #80f7f0;
            }}
            .btn-confirm {{
                background: linear-gradient(135deg, #a78bfa 0%, #80f7f0 100%);
                color: black !important; /* Testo nero anche nel tema scuro */
            }}
            .footer {{
                color: #a0a0a0;
                border-top: 1px solid #333333;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>🎮 Conferma il tuo account 🎮</div>

        <p>Ciao <span class='username'>{username}</span>,</p>

        <p>Benvenuto nella nostra gaming community! Per completare la registrazione, conferma il tuo indirizzo email cliccando sul pulsante qui sotto:</p>

        <center>
            <a href='{confermaUrl}' class='btn-confirm'>CONFERMA EMAIL</a>
        </center>

        <p>Se non hai richiesto la registrazione, puoi ignorare questa email.</p>

        <div class='footer'>
            <p>A presto in-game!<br>
            <strong>Il Team di Gaming Store</strong></p>
            <p style='font-size:12px;margin-top:10px;'>© {DateTime.Now.Year} Gaming Store. Tutti i diritti riservati.</p>
        </div>
    </div>
</body>
</html>";
            await SendEmailAsync(email, subject, body);
        }

        public Task SendEmailAsync(string to, string from, string subject, string htmlMessage)
        {
            throw new NotImplementedException();
        }

        // Il metodo Task SendEmailAsync(string to, string from, string subject, string htmlMessage)
        // è stato rimosso dall'interfaccia e dall'implementazione perché era un duplicato
        // con parametri invertiti rispetto al primo SendEmailAsync, e non utilizzato.
    }
}

// La classe EmailSettings rimane invariata
public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int MailPort { get; set; } = 587;
    public string SenderName { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30000;
}