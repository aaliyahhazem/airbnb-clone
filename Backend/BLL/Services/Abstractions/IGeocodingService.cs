
namespace BLL.Services.Abstractions
{

        public interface IGeocodingService
        {
            Task<GeocodeResult?> GeocodeAddressAsync(string address);
            Task<GeocodeResult?> ReverseGeocodeAsync(double latitude, double longitude);
        }

        public class GeocodeResult
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string FormattedAddress { get; set; } = string.Empty;
            public string Country { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string Street { get; set; } = string.Empty;
            public string PostalCode { get; set; } = string.Empty;
        }

}
