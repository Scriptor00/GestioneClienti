using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GestioneClienti.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
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
            // controllo l'esistenza dell'utente tramite la sua mail(unique)
            var existingUser = await _context.Clienti
                                              .FirstOrDefaultAsync(c => c.Email == model.Email);
            if (existingUser != null)
                return false; // Se l'utente esiste già, ritorna false

            var cliente = new Cliente
            {
                Nome = model.Nome,
                Cognome = model.Cognome,
                Email = model.Email,
                // Password = model.Password // La password è salvata in chiaro (da proteggere in futuro)
            };

            await _context.Clienti.AddAsync(cliente);
            await _context.SaveChangesAsync();

            return true; // Registrazione avvenuta con successo
        }

    }
}