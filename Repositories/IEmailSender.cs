namespace GestioneClienti.Repositories
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
        Task SendEmailAsync(string to, string from, string subject, string htmlMessage);
        Task SendWelcomeEmail(string email, string username);
        Task SendEmailConferma(string email, string username, string token);
        Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, byte[] attachmentBytes, string attachmentFileName, string attachmentContentType);

    }
}
