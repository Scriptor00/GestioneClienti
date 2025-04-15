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
using System.Text;
using System.Text.Encodings.Web;
using GestioneClienti.Repositories;
using Microsoft.Extensions.Logging;
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
                    .Where(u => u.Username == model.Username)
                    .Select(u => new
                    {
                        u.Id, 
                        u.Username,
                        u.PasswordHash,
                        u.Role,
                        u.Email
                    })
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (utente == null)
                {
                   
                    _logger.LogWarning($"Tentativo di login fallito per username: {model.Username}");
                    ModelState.AddModelError(string.Empty, "Credenziali non valide");
                    return View(model);
                }

                // Verifica password con timing costante
                var passwordValida = VerifyPassword(model.Password, utente.PasswordHash);

                if (!passwordValida)
                {
                    _logger.LogWarning($"Password errata per l'utente: {model.Username}");
                    ModelState.AddModelError(string.Empty, "Credenziali non valide");
                    return View(model);
                }


                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, utente.Username),
                    new Claim(ClaimTypes.Name, utente.Username),
                    new Claim(ClaimTypes.Email, utente.Email ?? string.Empty),
                    new Claim(ClaimTypes.Role, utente.Role ?? "User"),
                    new Claim("UserId", utente.Id.ToString()) // Aggiungi l'Id come claim
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
                RecaptchaToken = string.Empty
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

           var recaptchaValid = await _recaptchaService.VerifyTokenAsync(model.RecaptchaToken);
           if (!recaptchaValid)
           {
             ModelState.AddModelError(string.Empty, "Verifica reCAPTCHA non superata. Riprova.");
            _logger.LogWarning("Verifica reCAPTCHA fallita");
             return View(model);
            }

            try
            {
                // verifica esistenza username o email
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

                // Crea nuovo utente
                var newUser = new Utente
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = "User", // ruolo di default
                    PasswordResetToken = null,
                    PasswordResetTokenExpires = null
                };

                _context.Utenti.Add(newUser);
                await _context.SaveChangesAsync();

                // Opzionale: Invia email di benvenuto
                // await _emailService.SendWelcomeEmail(model.Email, model.Username);

                _logger.LogInformation($"Nuovo utente registrato: {model.Username} ({model.Email})");

                TempData["SuccessMessage"] = "Registrazione completata con successo!";
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

                var emailBody = $"Per reimpostare la tua password, clicca qui: <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Reimposta Password</a>";

                await _emailSender.SendEmailAsync(model.Email, "Recupero Password", emailBody);

                ViewBag.Messaggio = "Se l'indirizzo email fornito è valido, ti abbiamo inviato un link per reimpostare la password.";
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
                Email = utente.Email, // Potresti ancora aver bisogno dell'email (readonly?)
                Token = token,
                UserId = userId.Value, // Passa l'UserId al ViewModel
                NuovaPassword = string.Empty, // Inizializza le proprietà per il form
                ConfermaPassword = string.Empty
            };

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
            return RedirectToAction("Login");
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

                // 6. Logout forzato (se vuoi disconnettere l'utente dopo il cambio password)
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