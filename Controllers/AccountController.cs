using Microsoft.AspNetCore.Mvc;
using GestioneClienti.ViewModel;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using WebAppEF.Models;
using Microsoft.EntityFrameworkCore;

namespace GestioneClienti.Controllers
{
    public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
    {
        _context = context;
        _logger = logger;
    }

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

    // Cerca l'utente nel database
    var utente = await _context.Utenti
        .FirstOrDefaultAsync(u => u.Username == model.Username);

    if (utente == null)
    {
        _logger.LogWarning($"Tentativo di login fallito per {model.Username}: utente non trovato.");
        ModelState.AddModelError(string.Empty, "Credenziali errate");
        return View(model);
    }

    // Log per debug: stampa la password fornita e l'hash memorizzato
    _logger.LogDebug($"Password fornita: {model.Password}");
    _logger.LogDebug($"Hash memorizzato: {utente.PasswordHash}");

    // Verifica la password
    var passwordValida = VerifyPassword(model.Password, utente.PasswordHash);

    if (!passwordValida)
    {
        _logger.LogWarning($"Tentativo di login fallito per {model.Username}: password errata.");
        ModelState.AddModelError(string.Empty, "Credenziali errate");
        return View(model);
    }

    // Creazione dei claim per l'utente
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, utente.Username),
        new Claim(ClaimTypes.Role, utente.Role)
    };

    // Creazione dell'identità e del principal
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    // Configura le proprietà di autenticazione
    var authProperties = new AuthenticationProperties
    {
        IsPersistent = model.RememberMe, // Mantieni la sessione attiva se l'utente seleziona "ricordami"
        ExpiresUtc = model.RememberMe
            ? DateTimeOffset.UtcNow.AddDays(30)
            : DateTimeOffset.UtcNow.AddMinutes(30)
    };

    // Firma l'utente
    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

    _logger.LogInformation($"L'utente {utente.Username} ha effettuato l'accesso con successo.");

    TempData["WelcomeMessage"] = "Benvenuto nella tua Dashboard!";

    return RedirectToAction("Dashboard", "Home");
}

        private static bool VerifyPassword(string password, string passwordHash)
        {
               return BCrypt.Net.BCrypt.Verify(password, passwordHash);

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
                Nome = string.Empty,
                Cognome = string.Empty,
                Email = string.Empty,
                Password = string.Empty,
                ConfermaPassword = string.Empty
            });

        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Login");
            }

            return View(model);
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
    }
}

