using Microsoft.AspNetCore.Mvc;
using GestioneClienti.ViewModel;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebAppEF.Models;
using Microsoft.EntityFrameworkCore;
using GestioneClienti.Entities;
using Microsoft.AspNetCore.Authorization;

namespace GestioneClienti.Controllers
{
    public class AccountController(ApplicationDbContext context, ILogger<AccountController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<AccountController> _logger = logger;

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

    var utente = await _context.Utenti.FirstOrDefaultAsync(u => u.Username == model.Username);

    if (utente == null)
    {
        _logger.LogWarning($"Tentativo di login fallito per {model.Username}: utente non trovato.");
        ModelState.AddModelError(string.Empty, "Credenziali errate");
        return View(model);
    }

    _logger.LogDebug($"Password fornita: {model.Password}");
    _logger.LogDebug($"Hash memorizzato: {utente.PasswordHash}");

    var passwordValida = VerifyPassword(model.Password, utente.PasswordHash);

    if (!passwordValida)
    {
        _logger.LogWarning($"Tentativo di login fallito per {model.Username}: password errata.");
        ModelState.AddModelError(string.Empty, "Credenziali errate");
        return View(model);
    }

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, utente.Username),
        new Claim(ClaimTypes.Role, utente.Role)
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    var authProperties = new AuthenticationProperties
    {
        IsPersistent = model.RememberMe,
        ExpiresUtc = model.RememberMe
            ? DateTimeOffset.UtcNow.AddDays(30)
            : DateTimeOffset.UtcNow.AddMinutes(30)
    };

    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

    _logger.LogInformation($"L'utente {utente.Username} ha effettuato l'accesso con successo.");

    TempData["WelcomeMessage"] = "Benvenuto nella tua Dashboard!";

    if (utente.Role == "Admin")
    {
        return RedirectToAction("Dashboard", "Home");
    }
    else
    {
        return RedirectToAction("Home", "Prodotti");
    }
}

private static bool VerifyPassword(string password, string passwordHash)
{
   // _logger.LogDebug($"Verifica password: password={password}, hash={passwordHash}"); // Log di debug aggiuntivo
    var result = BCrypt.Net.BCrypt.Verify(password, passwordHash);
   // _logger.LogDebug($"Verifica password risultato: {result}"); // Log di debug aggiuntivo
    return result;
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
                Password = string.Empty,
                ConfermaPassword = string.Empty
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

            try
            {
                // Verifica se l'username è già in uso
                var existingUser = await _context.Utenti
                    .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Username già in uso");
                    _logger.LogWarning($"Tentativo di registrazione fallito: username {model.Username} già esistente");
                    return View(model);
                }

                // Crea nuovo utente
                var newUser = new Utente
                {
                    Username = model.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = "User" // Ruolo di default
                };

                // Salva nel database
                _context.Utenti.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Nuovo utente registrato: {model.Username}");

                TempData["SuccessMessage"] = "Registrazione completata con successo!";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la registrazione");
                ModelState.AddModelError(string.Empty, "Si è verificato un errore durante la registrazione. Riprova più tardi.");
                return View(model);
            }
        }
        [HttpGet]
        public IActionResult RecuperoPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RecuperoPassword(string email)
        {
            TempData["Message"] = "Se l'email è registrata, riceverai un'email con le istruzioni per il recupero della password.";
            return RedirectToAction("RecuperoPassword");
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
    if (!ModelState.IsValid) return View(model);

    var username = User.Identity?.Name;
    if (string.IsNullOrEmpty(username))
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Login");
    }

    try
    {
        // 1. Carica utente con lock esplicito
        var utente = await _context.Utenti
            .FromSqlRaw("SELECT * FROM Utenti WITH (UPDLOCK, ROWLOCK) WHERE Username = {0}", username)
            .FirstOrDefaultAsync();

        if (utente == null)
        {
            ModelState.AddModelError(string.Empty, "Utente non trovato");
            return View(model);
        }

        // 2. Verifica password corrente
        if (!BCrypt.Net.BCrypt.Verify(model.PasswordCorrente, utente.PasswordHash))
        {
            ModelState.AddModelError("CurrentPassword", "Password corrente non valida");
            return View(model);
        }

        // 3. Genera nuovo hash con work factor esplicito
        var newHash = BCrypt.Net.BCrypt.HashPassword(model.NuovaPassword, 12); // 12 è il work factor

        // 4. Aggiornamento diretto con SQL esplicito
        var updateSql = "UPDATE Utenti SET PasswordHash = {0} WHERE Id = {1}";
        var rowsAffected = await _context.Database.ExecuteSqlRawAsync(updateSql, newHash, utente.Id);

        if (rowsAffected != 1)
        {
            throw new Exception("Nessuna riga aggiornata");
        }

        // 5. Verifica immediata che l'update sia avvenuto
        var updatedHash = await _context.Utenti
            .Where(u => u.Id == utente.Id)
            .Select(u => u.PasswordHash)
            .FirstOrDefaultAsync();

        if (updatedHash != newHash)
        {
            throw new Exception("Hash non corrispondente dopo l'update");
        }

        // 6. Logout forzato
        await HttpContext.SignOutAsync();

        // 7. Logging di conferma
        _logger.LogInformation($"Password cambiata con successo per {username}. Nuovo hash: {updatedHash}");

        TempData["SuccessMessage"] = "Password cambiata con successo! Accedi con la nuova password.";
        return RedirectToAction("Login");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Errore durante cambio password per {username}");
        ModelState.AddModelError(string.Empty, "Errore durante l'operazione. Riprova.");
        return View(model);
    }
}
    }
}

