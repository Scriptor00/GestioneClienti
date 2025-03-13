using System.ComponentModel.DataAnnotations;
using WebAppEF.Entities;

namespace WebAppEF.ViewModels
{
    public class OrdineViewModel
    {
        public int IdOrdine { get; set; }
        
        [Required(ErrorMessage = "È necessario selezionare un cliente.")]
        [Display(Name = "Cliente")] 
        public int IdCliente { get; set; }
        public ClienteViewModel? Cliente { get; set; } 
        
        [Required(ErrorMessage = "Lo stato dell'ordine è obbligatorio.")]
        public StatoOrdine Stato { get; set; }
        
        [Required(ErrorMessage = "Il totale dell'ordine è obbligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Il totale deve essere maggiore di zero.")]
        [Display(Name = "Totale Ordine")]
        public decimal TotaleOrdine { get; set; }
        
        [Required(ErrorMessage = "La data dell'ordine è obbligatoria.")]
        [Display(Name = "Data Ordine")]
        [DataType(DataType.Date)]
        public DateTime DataOrdine { get; set; }
        public List<ClienteViewModel>? Clienti { get; set; }
    }
}