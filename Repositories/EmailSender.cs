using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GestioneClienti.Repositories
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IOptions<EmailSettings> emailSettings, ILogger<EmailSender> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                _logger.LogInformation("Preparazione invio email a {Email}", email);
                _logger.LogDebug("Configurazione SMTP: Server={SmtpServer}:{SmtpPort}",
                    _emailSettings.SmtpServer, _emailSettings.MailPort);

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.MailPort, SecureSocketOptions.StartTls);

                    await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);

                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.FromEmail));
                    message.To.Add(new MailboxAddress("", email));
                    message.Subject = subject;

                    var bodyBuilder = new BodyBuilder
                    {
                        HtmlBody = htmlMessage
                    };
                    message.Body = bodyBuilder.ToMessageBody();

                    _logger.LogInformation("Invio email a {Email} con oggetto '{Subject}'", email, subject);

                    await client.SendAsync(message);

                    await client.DisconnectAsync(true);

                    _logger.LogInformation("Email inviata con successo a {Email}", email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'invio a {Email}", email);
                throw;
            }
        }

        public async Task SendWelcomeEmail(string email, string username)
        {
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
           <a href='http://localhost:5000/Prodotti/Home' class='btn'>INIZIA L'AVVENTURA</a>
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

        public async Task SendEmailConferma(string email, string username, string token)
        {
            var confermaUrl = $"http://localhost:5000/Account/ConfermaEmail?token={token}";
            var subject = "Conferma la tua registrazione";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            background-color: #0f0f1a;
            color: #e0e0ff;
            font-family: 'Segoe UI', Arial, sans-serif;
            line-height: 1.6;
            padding: 20px;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #1a1a2e;
            padding: 30px;
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
        .btn-confirm {{
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
            border-top: 1px solid #2d2d5a;
            padding-top: 20px;
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
</html>
";
            await SendEmailAsync(email, subject, body);
        }

        public Task SendEmailAsync(string to, string from, string subject, string htmlMessage)
        {
            throw new NotImplementedException();
        }
    }
}

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
    Task SendWelcomeEmail(string email, string username);
    Task SendEmailConferma(string email, string username, string token);
}

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