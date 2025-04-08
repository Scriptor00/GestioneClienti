using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestioneClienti.Repositories
{
    public interface IGeocodingService
    {
        Task<List<GeocodingResult>> GetAddressSuggestionsAsync(string query, string region = null);
        Task<List<NominatimAddress>> AutocompleteIndirizzoAsync(string query, string countryCode = null, int limit = 10);
    }
}