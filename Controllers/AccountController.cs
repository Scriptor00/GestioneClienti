using Microsoft.AspNetCore.Mvc;
using GestioneClienti.ViewModel;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebAppEF.Models;
using Microsoft.EntityFrameworkCore;
using GestioneClienti.Entities;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Encodings.Web;
using GestioneClienti.Repositories;
using GestioneClienti.Services;

namespace GestioneClienti.Controllers
{
    public class AccountController(ApplicationDbContext context, ILogger<AccountController> logger, IEmailSender emailSender, RecaptchaService recaptchaService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<AccountController> _logger = logger;
        private readonly IEmailSender _emailSender = emailSender;
        private readonly RecaptchaService _recaptchaService = recaptchaService;


        [HttpGet]
        public IActionResult Login()
        {
            if (TempData["EmailConfermata"] != null)
            {
                ViewBag.EmailConfermata = true;
            }

            return View(new LoginViewModel
            {
                Username = string.Empty,
                Password = string.Empty,
                RememberMe = false
            });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Tentativo di login con dati non validi.");
                return View(model);
            }

            try
            {
                var utente = await _context.Utenti
                    .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (utente == null)
                {
                    _logger.LogWarning($"Tentativo di login fallito per username inesistente: {model.Username}");
                    ModelState.AddModelError(string.Empty, "Credenziali non valide");
                    return View(model);
                }

                // Controllo blocco account
                if (utente.LockoutEnabled && utente.LockoutEnd.HasValue && utente.LockoutEnd > DateTime.UtcNow)
                {
                    var minutiRestanti = (utente.LockoutEnd.Value - DateTime.UtcNow).Minutes;
                    ModelState.AddModelError(string.Empty, $"Account bloccato. Riprova tra {minutiRestanti} minuti.");
                    return View(model);
                }

                // Verifica password
                var passwordValida = VerifyPassword(model.Password, utente.PasswordHash);

                if (!passwordValida)
                {
                    utente.AccessFailedCount++;

                    if (utente.AccessFailedCount >= 5)
                    {
                        utente.LockoutEnabled = true;
                        utente.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
                        _logger.LogWarning($"Account bloccato per 10 minuti: {utente.Username}");
                        ModelState.AddModelError(string.Empty, "Hai superato i tentativi massimi. Account bloccato per 10 minuti.");
                    }
                    else
                    {
                        _logger.LogWarning($"Tentativo fallito {utente.AccessFailedCount}/5 per: {utente.Username}");
                        ModelState.AddModelError(string.Empty, "Credenziali non valide.");
                    }

                    await _context.SaveChangesAsync();
                    return View(model);
                }

                // dopo il login reimposto tutto
                utente.AccessFailedCount = 0;
                utente.LockoutEnabled = false;
                utente.LockoutEnd = null;

                if (!utente.EmailConfermata && utente.Role != "Admin")
                {
                    _logger.LogWarning($"L'utente {model.Username} ha tentato di accedere senza confermare l'email.");
                    ModelState.AddModelError(string.Empty, "Devi confermare la tua email prima di poter accedere.");
                    return View(model);
                }

                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, utente.Username),
            new Claim(ClaimTypes.Name, utente.Username),
            new Claim(ClaimTypes.Email, utente.Email ?? string.Empty),
            new Claim(ClaimTypes.Role, utente.Role ?? "User"),
            new Claim("UserId", utente.Id.ToString())
        };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddMinutes(30),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProperties);

                _logger.LogInformation($"Login riuscito per: {utente.Username}");

                await _context.SaveChangesAsync();

                model.Password = string.Empty;

                TempData["WelcomeMessage"] = $"Benvenuto, {utente.Username}!";

                if (utente.Role == "Admin")
                {
                    return RedirectToAction("Dashboard", "Home");
                }
                else
                {
                    return RedirectToAction("Home", "Prodotti");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante il login per {model.Username}");
                ModelState.AddModelError(string.Empty, "Si è verificato un errore durante l'accesso");
                return View(model);
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(storedHash))
                {
                    _logger.LogWarning("Hash password non valido");
                    return false;
                }

                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nella verifica della password");
                return false;
            }
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User logged out");
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel
            {
                Username = string.Empty,
                Email = string.Empty,
                Password = string.Empty,
                ConfermaPassword = string.Empty,
                RecaptchaResponse = string.Empty
            });
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Tentativo di registrazione con dati non validi");
                return View(model);
            }

            var recaptchaValid = await _recaptchaService.VerifyTokenAsync(model.RecaptchaResponse);
            if (!recaptchaValid)
            {
                ModelState.AddModelError(string.Empty, "Verifica reCAPTCHA non superata. Riprova.");
                _logger.LogWarning("Verifica reCAPTCHA fallita");
                return View(model);
            }

            try
            {
                var existingUser = await _context.Utenti
                    .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);

                if (existingUser != null)
                {
                    if (existingUser.Username == model.Username)
                    {
                        ModelState.AddModelError("Username", "Username già in uso");
                        _logger.LogWarning($"Username {model.Username} già esistente");
                    }

                    if (existingUser.Email == model.Email)
                    {
                        ModelState.AddModelError("Email", "Email già registrata");
                        _logger.LogWarning($"Email {model.Email} già registrata");
                    }

                    return View(model);
                }

                var emailToken = Guid.NewGuid().ToString();

                var newUser = new Utente
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = "User",
                    EmailConfermata = false,
                    EmailConfermaToken = emailToken,
                    PasswordResetToken = null,
                    PasswordResetTokenExpires = null
                };

                _context.Utenti.Add(newUser);
                await _context.SaveChangesAsync();

                await _emailSender.SendEmailConferma(model.Email, model.Username, emailToken);

                _logger.LogInformation($"Nuovo utente registrato: {model.Username} ({model.Email})");

                TempData["SuccessMessage"] = "Registrazione completata! Controlla la tua email per confermare l’account.";
                return RedirectToAction("Login");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Errore database durante la registrazione");
                ModelState.AddModelError(string.Empty, "Si è verificato un errore durante il salvataggio dei dati.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore generico durante la registrazione");
                ModelState.AddModelError(string.Empty, "Si è verificato un errore imprevisto. Riprova più tardi.");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfermaEmail(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token di conferma mancante o non valido.");
                TempData["ErrorMessage"] = "Token di conferma non valido.";
                return RedirectToAction("Login");
            }

            try
            {
                var user = await _context.Utenti.FirstOrDefaultAsync(u => u.EmailConfermaToken == token && !u.EmailConfermata);

                if (user == null)
                {
                    _logger.LogWarning("Token non valido o utente già confermato.");
                    TempData["ErrorMessage"] = "Il link di conferma non è valido o l'account è già confermato.";
                    return RedirectToAction("Login");
                }

                // Conferma l'email
                user.EmailConfermata = true;
                user.EmailConfermaToken = null; // Rimozione token di conferma
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Email confermata per l'utente: {user.Username} ({user.Email})");

                await _emailSender.SendWelcomeEmail(user.Email, user.Username);
                TempData["SuccessMessage"] = "Email confermata con successo!";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la conferma dell'email.");
                TempData["ErrorMessage"] = "Si è verificato un errore. Riprova più tardi.";
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public IActionResult RecuperoPassword()
        {
            return View(new RichiestaRecuperoPasswordViewModel());
        }
        private string HashPassword(string password)
        {
            try
            {
                return BCrypt.Net.BCrypt.HashPassword(password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'hashing della password");
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecuperoPasswordInviaEmail(RichiestaRecuperoPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Utenti.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    ViewBag.Messaggio = "Se l'indirizzo email fornito è valido, ti abbiamo inviato un link per reimpostare la password.";
                    return View("RecuperoPassword");
                }

                var token = GeneratePasswordResetToken();
                user.PasswordResetToken = token;
                user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(24);
                _context.Update(user);
                await _context.SaveChangesAsync();

                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, token }, protocol: Request.Scheme);
                var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            line-height: 1.6;
            margin: 0;
            padding: 20px;
            background-color: #f5f5f5 !important;
            color: #333333 !important;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            border: 1px solid #e0e0e0 !important;
            border-radius: 8px;
            overflow: hidden;
            background-color: #ffffff !important;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(90deg, #6e48aa 0%, #9d50bb 100%) !important;
            padding: 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            color: black !important;
            font-size: 28px;
            font-weight: bold;
            text-shadow: 0 2px 4px rgba(0,0,0,0.3);
        }}
        .body-content {{
            padding: 25px;
        }}
        .body-content p {{
            font-size: 16px;
            line-height: 1.6;
            color: #333333 !important;
            margin-bottom: 15px;
        }}
        .body-content p strong {{
            color: #9d50bb !important;
        }}
        .button-container {{
            text-align: center;
            margin: 25px 0;
        }}
        .reset-password-button {{
            display: inline-block;
            padding: 14px 30px;
            background: linear-gradient(90deg, #6e48aa 0%, #9d50bb 100%) !important;
            color: red !important;
            text-decoration: none;
            border-radius: 6px;
            font-weight: bold;
            font-size: 16px;
            box-shadow: 0 3px 10px rgba(110, 72, 170, 0.4);
            border: none;
            min-width: 220px;
            text-shadow: none !important;
        }}
        .footer {{
            background-color: #f0f0f0 !important;
            padding: 15px;
            text-align: center;
            font-size: 12px;
            color: #666666 !important;
            border-top: 1px solid #e0e0e0;
        }}
        .footer p {{
            margin: 5px 0;
        }}
        .footer a {{
            color: #6e48aa !important;
            text-decoration: none;
            margin: 0 10px;
            font-weight: bold;
        }}
        .note {{
            font-size: 14px;
            color: #666666 !important;
            margin-top: 20px;
        }}
        .note strong {{
            color: #9d50bb !important;
        }}

        @media (prefers-color-scheme: dark) {{
            .reset-password-button {{
                background: linear-gradient(90deg, #6e48aa 0%, #9d50bb 100%) !important;
                color: red !important;
                text-shadow: none !important;
            }}
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🔐 Recupero Password</h1>
        </div>
        <div class=""body-content"">
            <p>Ciao <strong></strong>,</p>
            <p>Hai richiesto di reimpostare la password del tuo account. Clicca il pulsante qui sotto per procedere:</p>
            
            <div class=""button-container"">
                <a href=""{HtmlEncoder.Default.Encode(callbackUrl)}"" class=""reset-password-button"">
                    🎮 REIMPOSTA PASSWORD
                </a>
            </div>
            
            <p class=""note"">
                Se non hai richiesto tu questa operazione, ignora questa email.
            </p>
            <p class=""note"">
                Il link scadrà tra <strong>24 ore</strong>.
            </p>
        </div>
        <div class=""footer"">
            <p>© {DateTime.Now.Year} <strong>Gaming Store</strong>. Tutti i diritti riservati.</p>
            <p>
                <a href=""https://twitter.com/tuostore"">Twitter</a>
                <a href=""https://instagram.com/tuostore"">Instagram</a>
            </p>
        </div>
    </div>
</body>
</html>";


                await _emailSender.SendEmailAsync(model.Email, "Recupero Password", emailBody);

                TempData["MessaggioRecuperoPassword"] = "Ti abbiamo inviato un link per reimpostare la password all'indirizzo email fornito.";
                return View("RecuperoPassword");
            }

            return View(model);
        }

        private static string GeneratePasswordResetToken()
        {
            const int tokenSizeInBytes = 32; // Genera un token di 32 byte (256 bit)
            var bytes = RandomNumberGenerator.GetBytes(tokenSizeInBytes);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(int? userId, string token)
        {
            if (userId == null || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var utente = await _context.Utenti.FindAsync(userId);

            if (utente == null ||
                utente.PasswordResetToken != token || // Il token è già codificato in Base64Url nel DB
                utente.PasswordResetTokenExpires < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Il link per il reset non è valido o è scaduto.";
                return RedirectToAction("Login");
            }

            var model = new RecuperoPasswordViewModel
            {
                Email = utente.Email,
                Token = token,
                UserId = userId.Value,
                NuovaPassword = null,
                ConfermaPassword = null
            };

            ModelState.Clear();
            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(RecuperoPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var utente = await _context.Utenti.FindAsync(model.UserId); // Recupera l'utente per ID

            if (utente == null ||
                utente.PasswordResetToken != model.Token || // Confronta il token ricevuto (già Base64Url)
                utente.PasswordResetTokenExpires < DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "Il link per il reset non è valido o è scaduto.");
                return View(model);
            }

            // Hash della nuova password
            utente.PasswordHash = HashPassword(model.NuovaPassword);
            utente.PasswordResetToken = null;
            utente.PasswordResetTokenExpires = null;

            _context.Utenti.Update(utente);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Password aggiornata con successo!";
            return RedirectToAction("Login", new { forcedReload = DateTime.Now.Ticks });
        }


        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(CambioPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "Utente non autenticato" });
            }

            try
            {
                // 1. Carica utente con lock esplicito
                var utente = await _context.Utenti
                    .FromSqlRaw("SELECT * FROM Utenti WITH (UPDLOCK, ROWLOCK) WHERE Username = {0}", username)
                    .FirstOrDefaultAsync();

                if (utente == null)
                {
                    return NotFound(new { message = "Utente non trovato" });
                }

                // 2. Verifica password corrente
                if (!BCrypt.Net.BCrypt.Verify(model.PasswordCorrente, utente.PasswordHash))
                {
                    return BadRequest(new { message = "Password corrente non valida" });
                }

                // 3. Genera nuovo hash con work factor esplicito
                var newHash = BCrypt.Net.BCrypt.HashPassword(model.NuovaPassword, 12); // 12 è il work factor

                // 4. Aggiornamento diretto con SQL esplicito
                var updateSql = "UPDATE Utenti SET PasswordHash = {0} WHERE Id = {1}";
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(updateSql, newHash, utente.Id);

                if (rowsAffected != 1)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Errore durante l'aggiornamento della password" }); // Restituisce 500 come JSON
                }

                // 5. Verifica immediata che l'update sia avvenuto
                var updatedHash = await _context.Utenti
                    .Where(u => u.Id == utente.Id)
                    .Select(u => u.PasswordHash)
                    .FirstOrDefaultAsync();

                if (updatedHash != newHash)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Errore di verifica dopo l'aggiornamento della password" }); // Restituisce 500 come JSON
                }

                // 6. Logout forzato 
                await HttpContext.SignOutAsync();
                _logger.LogInformation($"Password cambiata con successo per {username}. Nuovo hash: {updatedHash}");
                return Json(new { success = true, message = "Password aggiornata con successo! Sarai reindirizzato per il login." });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante cambio password per {username}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Errore durante l'operazione. Riprova." }); // Restituisce 500 come JSON
            }
        }
    }
}