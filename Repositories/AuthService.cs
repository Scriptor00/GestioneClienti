using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GestioneClienti.Entities;
using GestioneClienti.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebAppEF.Entities;
using WebAppEF.Models;

namespace GestioneClienti.Repositories
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;


        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SignInAsync(string email)
        {
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, email)
        };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

#pragma warning disable CS8604 // Possible null reference argument.
            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
#pragma warning restore CS8604 // Possible null reference argument.
        }

        public async Task SignOutAsync()
        {
#pragma warning disable CS8604 // Possible null reference argument.
            await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
#pragma warning restore CS8604 // Possible null reference argument.
        }

        public async Task<bool> RegisterUserAsync(RegisterViewModel model)
        {
            try
            {
                // Verifica se l'username esiste già
                var existingUser = await _context.Utenti
                    .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (existingUser != null)
                {

                    return false;
                }

                // Crea il nuovo utente con password hashata
                var utente = new Utente
                {
                    Username = model.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password), // Hash della password
                    Role = "User" // Ruolo di default
                };

                await _context.Utenti.AddAsync(utente);
                await _context.SaveChangesAsync();


                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }

    }
}