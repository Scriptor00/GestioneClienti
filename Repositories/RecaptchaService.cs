using Newtonsoft.Json;

namespace GestioneClienti.Services
{
    public class RecaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        public RecaptchaService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _secretKey = configuration["GoogleRecaptcha:SecretKey"];
        }

        public async Task<bool> VerifyTokenAsync(string token)
        {
            var response = await _httpClient.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={_secretKey}&response={token}",
                null);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<RecaptchaResponse>(json);

            return result?.success ?? false;
        }

        private class RecaptchaResponse
        {
            public bool success { get; set; }
            public string challenge_ts { get; set; }
            public string hostname { get; set; }
            public string[] error_codes { get; set; }
        }
    }
}
