using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GestioneClienti.Repositories
{
    public class GeocodingResult
    {
        public string FormattedAddress { get; set; }
        public string Route { get; set; }
        public string StreetNumber { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
    }

    public class NominatimAddress
    {
        public string IndirizzoCompleto { get; set; }
        public string Via { get; set; }
        public string Civico { get; set; }
        public string Citta { get; set; }
        public string Paese { get; set; }
        public string Cap { get; set; }
    }

    
    public class NominatimGeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NominatimGeocodingService> _logger;
        private const string NominatimBaseUrl = "https://nominatim.openstreetmap.org/search";

        public NominatimGeocodingService(HttpClient httpClient, ILogger<NominatimGeocodingService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configurazione di base per Nominatim
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GestioneClienti/1.0");
        }

        public async Task<List<GeocodingResult>> GetAddressSuggestionsAsync(string query, string region = null)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            {
                _logger.LogWarning("Query di ricerca troppo corta: {Query}", query);
                return new List<GeocodingResult>();
            }

            try
            {
                _logger.LogDebug("Inizio ricerca indirizzi per query: {Query}", query);

                var url = $"{NominatimBaseUrl}?format=json&addressdetails=1&q={Uri.EscapeDataString(query)}";
                if (!string.IsNullOrEmpty(region))
                {
                    url += $"&countrycodes={region}";
                }

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<NominatimResponse>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var suggestions = results?.ConvertAll(r => new GeocodingResult
                {
                    FormattedAddress = r.DisplayName,
                    Route = r.Address?.Road,
                    StreetNumber = r.Address?.HouseNumber,
                    Country = r.Address?.Country,
                    PostalCode = r.Address?.Postcode,
                    City = r.Address?.City ?? r.Address?.Town ?? r.Address?.Village
                }) ?? new List<GeocodingResult>();

                _logger.LogDebug("Trovati {Count} suggerimenti per query: {Query}", suggestions.Count, query);
                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei suggerimenti per query: {Query}", query);
                return new List<GeocodingResult>();
            }
        }

        public async Task<List<NominatimAddress>> AutocompleteIndirizzoAsync(
            string query,
            string countryCode = null,
            int limit = 10)
        {
            var url = $"{NominatimBaseUrl}?format=json&addressdetails=1&limit={limit}&q={Uri.EscapeDataString(query)}";

            if (!string.IsNullOrEmpty(countryCode))
            {
                url += $"&countrycodes={countryCode.ToLower()}";
            }

            _logger.LogDebug("Chiamata a Nominatim con URL: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var nominatimResults = JsonSerializer.Deserialize<List<NominatimResponse>>(content, options);

            return nominatimResults?.Select(r => new NominatimAddress
            {
                IndirizzoCompleto = r.DisplayName, // Usa DisplayName come indirizzo completo iniziale
                Via = r.Address?.Road,
                Civico = r.Address?.HouseNumber,
                Citta = r.Address?.City ?? r.Address?.Town ?? r.Address?.Village,
                Paese = r.Address?.Country,
                Cap = r.Address?.Postcode
            }).ToList() ?? new List<NominatimAddress>();
        }

        public Task AutocompleteIndirizzoAsync(string query)
        {
            throw new NotImplementedException();
        }

        public Task<List<GeocodingResult>> GetAddressSuggestionsAsync(string query, string region = null, int limit = 10)
        {
            throw new NotImplementedException();
        }

        private class NominatimResponse
        {
            public string DisplayName { get; set; }
            public Address Address { get; set; }
        }

        private class Address
        {
            [JsonPropertyName("house_number")]
            public string HouseNumber { get; set; }
            public string Road { get; set; }
            public string Country { get; set; }
            public string Postcode { get; set; }
            public string City { get; set; }
            public string Town { get; set; }
            public string Village { get; set; }
        }
    }
}