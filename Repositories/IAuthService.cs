using GestioneClienti.ViewModel;

namespace GestioneClienti.Repositories
{
    public interface IAuthService
    {
        Task SignInAsync(string email);
        Task SignOutAsync();

        Task<bool> RegisterUserAsync(string email, RegisterViewModel model);
    }
}