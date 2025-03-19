
namespace WebAppEF.ViewModels
{
    public class ClienteViewModel
    {
        public int IdCliente { get; set; }
        public string Nome { get; set; }
        public string Cognome { get; set; }
        public string Email { get; set; }
        
        public static implicit operator ClienteViewModel(List<ClienteViewModel> v)
        {
            throw new NotImplementedException();
        }
    }
}
