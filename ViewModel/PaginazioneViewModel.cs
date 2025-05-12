using WebAppEF.Entities;

namespace WebAppEF.ViewModel
{
    public class PaginazioneViewModel
    {
    public List<Cliente>? Clienti { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }

    

    }
}