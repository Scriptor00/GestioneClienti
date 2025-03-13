namespace WebAppEF.ViewModel
{
    public class PagOrdiniViewModel<T>
    {
        public List<T> Ordini { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}
