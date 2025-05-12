using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestioneClienti.Repositories
{
    public interface IGeocodingService
    {
        Task<List<AddressSuggestion>> GetAddressSuggestionsAsync(string input, string region = null, int limit = 5);
        Task<AddressDetails> GetAddressDetailsAsync(string placeId);
        Task<AddressDetails> GetPlaceDetailsAsync(string placeId);

    }
}