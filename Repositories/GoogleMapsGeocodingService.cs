using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GestioneClienti.Repositories
{
    public class AddressSuggestion
    {
        public string Description { get; set; }
        public string PlaceId { get; set; }
    }

    public class AddressDetails
    {
        public string Street { get; set; }
        public string StreetNumber { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
    }



    public class GoogleMapsGeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GoogleMapsGeocodingService> _logger;

        public GoogleMapsGeocodingService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleMapsGeocodingService> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GoogleMaps:ApiKey"];
            _logger = logger;
        }

        private async Task<string> GetApiResponseAsync(string url)
        {
            _logger.LogDebug("Chiamata API Google Maps: {Url}", url);
            HttpResponseMessage response = null;
            string content = null;
            try
            {
                response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Risposta API Google Maps (Successo): {Content}", content);
                return content;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Errore nella chiamata API Google Maps per URL: {Url}. Status Code: {StatusCode}", url, response?.StatusCode);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout o cancellazione della chiamata API Google Maps per URL: {Url}", url);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore imprevisto durante la chiamata API Google Maps per URL: {Url}", url);
                return null;
            }
        }

        private List<AddressSuggestion> ParseAddressSuggestions(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("Ricevuta risposta vuota per i suggerimenti di indirizzo.");
                return new List<AddressSuggestion>();
            }

            try
            {
                using var doc = JsonDocument.Parse(content);
                if (!doc.RootElement.TryGetProperty("predictions", out var predictionsElement))
                {
                    _logger.LogWarning("La risposta API non contiene la proprietà 'predictions'. Contenuto: {Content}", content);
                    return new List<AddressSuggestion>();
                }

                var predictions = predictionsElement.EnumerateArray();
                var suggestions = predictions.Select(p => new AddressSuggestion
                {
                    Description = p.TryGetProperty("description", out var descriptionElement) ? descriptionElement.GetString() : null,
                    PlaceId = p.TryGetProperty("place_id", out var placeIdElement) ? placeIdElement.GetString() : null
                }).ToList();

                _logger.LogDebug("Trovati {Count} suggerimenti di indirizzo.", suggestions.Count);
                return suggestions;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Errore durante il parsing JSON dei suggerimenti di indirizzo. Contenuto: {Content}", content);
                return new List<AddressSuggestion>();
            }
        }

        private AddressDetails ParseAddressDetails(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("Ricevuta risposta vuota per i dettagli del luogo.");
                return new AddressDetails();
            }

            try
            {
                using var doc = JsonDocument.Parse(content);
                if (!doc.RootElement.TryGetProperty("result", out var resultElement))
                {
                    _logger.LogWarning("La risposta API non contiene la proprietà 'result'. Contenuto: {Content}", content);
                    return new AddressDetails();
                }

                if (!resultElement.TryGetProperty("address_components", out var addressComponents))
                {
                    _logger.LogWarning("La risposta API non contiene la proprietà 'address_components' nel 'result'. Contenuto: {Content}", content);
                    return new AddressDetails();
                }

                string GetComponent(string type)
                {
                    var component = addressComponents.EnumerateArray()
                        .FirstOrDefault(c => c.TryGetProperty("types", out var typesElement) && typesElement.EnumerateArray().Any(t => t.GetString() == type));

                    if (component.ValueKind != JsonValueKind.Undefined && component.TryGetProperty("long_name", out var longNameElement))
                    {
                        return longNameElement.GetString();
                    }

                    return null;
                }

                var details = new AddressDetails
                {
                    Street = GetComponent("route"),
                    StreetNumber = GetComponent("street_number"),
                    City = GetComponent("locality") ?? GetComponent("administrative_area_level_3"),
                    PostalCode = GetComponent("postal_code"),
                    Country = GetComponent("country")
                };

                _logger.LogDebug("Dettagli del luogo parsati: {@AddressDetails}", details);
                return details;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Errore durante il parsing JSON dei dettagli del luogo. Contenuto: {Content}", content);
                return new AddressDetails();
            }
        }

        public async Task<List<AddressSuggestion>> GetAddressSuggestionsAsync(string input, string region = null, int limit = 5)
        {
            _logger.LogInformation("Richiesta suggerimenti indirizzo per input: '{Input}', regione: '{Region}', limite: {Limit}", input, region, limit);
            var url = $"https://maps.googleapis.com/maps/api/place/autocomplete/json?input={Uri.EscapeDataString(input)}&types=address&key={_apiKey}";
            if (!string.IsNullOrEmpty(region))
            {
                url += $"&region={region}";
                _logger.LogDebug("Aggiunta la regione alla URL: {Url}", url);
            }

            var content = await GetApiResponseAsync(url);
            if (content == null)
            {
                return new List<AddressSuggestion>();
            }

            var suggestions = ParseAddressSuggestions(content).Take(limit).ToList();
            _logger.LogInformation("Trovati {Count} suggerimenti per input: '{Input}'", suggestions.Count, input);
            return suggestions;
        }

        public async Task<AddressDetails> GetPlaceDetailsAsync(string placeId)
        {
            _logger.LogInformation("Richiesta dettagli per placeId: '{PlaceId}'", placeId);
            var url = $"https://maps.googleapis.com/maps/api/place/details/json?place_id={placeId}&key={_apiKey}";
            var content = await GetApiResponseAsync(url);
            if (content == null)
            {
                return new AddressDetails();
            }
            return ParseAddressDetails(content);
        }

        public Task<AddressDetails> GetAddressDetailsAsync(string placeId)
        {
            _logger.LogWarning("Il metodo GetAddressDetailsAsync non è implementato e dovrebbe usare GetPlaceDetailsAsync.");
            return GetPlaceDetailsAsync(placeId);
        }
    }
}