using System.Net.Mail;
using System.Text.RegularExpressions;

namespace WebAppEF.Utilities
{
    // formato valido per email
    public class ValidazioneCliente
    {

        public static bool IsValidEmail(string email)
        {
            try
            {
                var mail = new MailAddress(email);
                return Regex.IsMatch(email, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // caratteri speciali 
        public static bool HasSpecialCharacters(string input)
        {
            // sono permesse solo lettere (a-z, A-Z), numeri (0-9), ".", "-", "_" e "@"
            return Regex.IsMatch(input, @"[^a-zA-Z0-9.\-_@]");
        }

        // Lunghezza dell'email
        public static bool LunghezzaMail(string email)
        {
            if (email.Length < 5 || email.Length > 255)
            {
                return false;
            }
            return true;
        }
    }
}
