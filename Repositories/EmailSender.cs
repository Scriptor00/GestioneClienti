using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace GestioneClienti.Repositories
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(EmailSettings emailSettings, ILogger<EmailSender> logger)
        {
            _emailSettings = emailSettings;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                _logger.LogInformation("Preparazione invio email a {Email}", email);
                _logger.LogDebug("Configurazione SMTP: Server={SmtpServer}:{SmtpPort}",
                    _emailSettings.SmtpServer, _emailSettings.MailPort); 

                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12; // Prova questa
                                                                                                         // System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls11; // Se la 12 non va
                                                                                                         // System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls;    // Se neanche la 11 va

                using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.MailPort)) // E anche qui
                {
                    client.EnableSsl = _emailSettings.UseSsl; // Usa l'impostazione dal JSON
                    client.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.Timeout = _emailSettings.Timeout;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_emailSettings.FromEmail, _emailSettings.SenderName),
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true,
                        Priority = MailPriority.High
                    };
                    mailMessage.To.Add(email);

                    _logger.LogInformation("Invio email a {Email} con oggetto '{Subject}'",
                        email, subject);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation("Email inviata con successo a {Email}", email);
                }
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "Errore SMTP durante l'invio a {Email}. Status: {StatusCode}",
                    email, smtpEx.StatusCode);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'invio a {Email}", email);
                throw;
            }
        }

        public Task SendEmailAsync(string to, string from, string subject, string htmlMessage)
        {
            throw new NotImplementedException();
        }
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int MailPort { get; set; } = 2525;
        public string SenderName { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool UseSsl { get; set; } = true;
        public int Timeout { get; set; } = 30000;
    }
}