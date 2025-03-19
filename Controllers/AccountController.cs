using Microsoft.AspNetCore.Mvc;
using GestioneClienti.ViewModel;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GestioneClienti.Controllers
{
    public class AccountController(ILogger<AccountController> logger) : Controller
    {
        private readonly ILogger<AccountController> _logger = logger;

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Tentativo di login con dati non validi.");
                return View(model);
            }

            // Credenziali hardcoded in fase di test
            var usernameValido = "carlo00";
            var passwordValida = "password";

            if (model.Username != usernameValido || model.Password != passwordValida)
            {
                _logger.LogWarning($"Tentativo di login fallito per {model.Username}");
                ModelState.AddModelError(string.Empty, "Credenziali errate");
                return View(model);
            }

            // Creazione dei claim per l'utente
            var claims = new List<Claim>
        {
        new Claim(ClaimTypes.Name, model.Username),
        };

            // Creazione dell'identità e del principal
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // le proprietà di autenticazione
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe, // Mantieni la sessione attiva se l'utente seleziona ricordami
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)  
                    : DateTimeOffset.UtcNow.AddMinutes(30)  
            };

            // Firma l'utente
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            _logger.LogInformation($"L'utente {model.Username} ha effettuato l'accesso con successo.");

            TempData["WelcomeMessage"] = "Benvenuto nella tua Dashboard!";

            return RedirectToAction("Dashboard", "Home");
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
            return View(new RegisterViewModel());
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
