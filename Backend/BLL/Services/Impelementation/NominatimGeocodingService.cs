using System.Text.Json.Serialization;
using System.Text.Json;



namespace BLL.Services.Impelementation
{
    public class NominatimGeocodingService : IGeocodingService
    {

        private readonly HttpClient _httpClient;
        private const string NominatimApiUrl = "https://nominatim.openstreetmap.org";
        private static readonly SemaphoreSlim _rateLimiter = new SemaphoreSlim(1, 1);
        private static DateTime _lastRequestTime = DateTime.MinValue;

        public NominatimGeocodingService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "AirbnbClone/1.0 (abdelrahmanhamed559@gmail.com)");

        }

        public async Task<GeocodeResult?> GeocodeAddressAsync(string address)
        {
            try
            {
                await EnforceRateLimitAsync();

                var url = $"{NominatimApiUrl}/search?q={Uri.EscapeDataString(address)}&format=json&addressdetails=1&limit=1";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<NominatimSearchResult>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (results != null && results.Any())
                {
                    return ParseSearchResult(results.First());
                }

                return null;

            }
            catch (Exception ex)
            {

                throw new Exception($"Geocoding failed: {ex.Message}", ex);
            }
        }

        public async Task<GeocodeResult?> ReverseGeocodeAsync(double latitude, double longitude)
        {
            try
            {
                await EnforceRateLimitAsync();

                var url = $"{NominatimApiUrl}/reverse?lat={latitude}&lon={longitude}&format=json&addressdetails=1";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<NominatimReverseResult>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result != null)
                {
                    return ParseReverseResult(result);
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Reverse geocoding failed: {ex.Message}", ex);
            }
        }

        private async Task EnforceRateLimitAsync()
        {
            await _rateLimiter.WaitAsync();
            try
            {
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                if (timeSinceLastRequest.TotalMilliseconds < 1000)
                {
                    var delay = 1000 - (int)timeSinceLastRequest.TotalMilliseconds;
                    await Task.Delay(delay);
                }
                _lastRequestTime = DateTime.UtcNow;
            }
            finally
            {
                _rateLimiter.Release();
            }
        }

        private GeocodeResult ParseSearchResult(NominatimSearchResult result)
        {
            return new GeocodeResult
            {
                Latitude = double.Parse(result.Lat),
                Longitude = double.Parse(result.Lon),
                FormattedAddress = result.DisplayName,
                Country = result.Address?.Country ?? string.Empty,
                City = result.Address?.City ?? result.Address?.Town ?? result.Address?.Village ?? string.Empty,
                Street = result.Address?.Road ?? string.Empty,
                PostalCode = result.Address?.Postcode ?? string.Empty
            };
        }

        private GeocodeResult ParseReverseResult(NominatimReverseResult result)
        {
            return new GeocodeResult
            {
                Latitude = double.Parse(result.Lat),
                Longitude = double.Parse(result.Lon),
                FormattedAddress = result.DisplayName,
                Country = result.Address?.Country ?? string.Empty,
                City = result.Address?.City ?? result.Address?.Town ?? result.Address?.Village ?? string.Empty,
                Street = result.Address?.Road ?? string.Empty,
                PostalCode = result.Address?.Postcode ?? string.Empty
            };
        }

        private class NominatimSearchResult
        {
            [JsonPropertyName("lat")]
            public string Lat { get; set; } = string.Empty;

            [JsonPropertyName("lon")]
            public string Lon { get; set; } = string.Empty;

            [JsonPropertyName("display_name")]
            public string DisplayName { get; set; } = string.Empty;

            [JsonPropertyName("address")]
            public NominatimAddress? Address { get; set; }
        }

        private class NominatimReverseResult
        {
            [JsonPropertyName("lat")]
            public string Lat { get; set; } = string.Empty;

            [JsonPropertyName("lon")]
            public string Lon { get; set; } = string.Empty;

            [JsonPropertyName("display_name")]
            public string DisplayName { get; set; } = string.Empty;

            [JsonPropertyName("address")]
            public NominatimAddress? Address { get; set; }
        }

        private class NominatimAddress
        {
            [JsonPropertyName("road")]
            public string? Road { get; set; }

            [JsonPropertyName("city")]
            public string? City { get; set; }

            [JsonPropertyName("town")]
            public string? Town { get; set; }

            [JsonPropertyName("village")]
            public string? Village { get; set; }

            [JsonPropertyName("country")]
            public string? Country { get; set; }

            [JsonPropertyName("postcode")]
            public string? Postcode { get; set; }
        }
    }
}
