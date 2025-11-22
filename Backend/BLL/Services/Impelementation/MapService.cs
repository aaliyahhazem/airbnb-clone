using BLL.ModelVM.MapsVM;

namespace BLL.Services.Impelementation
{

    public class MapService : IMapService
    {
        private readonly IMapRepo _mapRepo;
        private readonly IGeocodingService _geocodingService;

        public MapService(IMapRepo mapRepo, IGeocodingService geocodingService)
        {
            _mapRepo = mapRepo;
            _geocodingService = geocodingService;
        }

        public async Task<GeocodeResponseDto?> GeocodeAddressAsync(string address)
        {
            var result = await _geocodingService.GeocodeAddressAsync(address);
            if (result == null)
                return null;

            return new GeocodeResponseDto
            {
                Latitude = result.Latitude,
                Longitude = result.Longitude,
                FormattedAddress = result.FormattedAddress,
                Country = result.Country,
                City = result.City,
                Street = result.Street
            };
        }

        public async Task<MapSearchResponseDto> SearchPropertiesOnMapAsync(MapSearchRequestDto request)
        {

            // Validate bounding box ranges
            if (request.NorthEastLat < -90 || request.NorthEastLat > 90 ||
                request.SouthWestLat < -90 || request.SouthWestLat > 90 ||
                request.NorthEastLng < -180 || request.NorthEastLng > 180 ||
                request.SouthWestLng < -180 || request.SouthWestLng > 180)
            {
                throw new ArgumentException("Latitude or Longitude values out of range.");
            }

            if (request.NorthEastLat <= request.SouthWestLat ||
                request.NorthEastLng <= request.SouthWestLng)
            {
                throw new ArgumentException("Invalid bounding box coordinates.");
            }

            var properties = await _mapRepo.GetPropertiesInBoundsAsync(
                request.NorthEastLat,
                request.NorthEastLng,
                request.SouthWestLat,
                request.SouthWestLng
            );

            var propertyDtos = properties.Select(p => new PropertyMapDto
            {
                Id = p.Id,
                Title = p.Title,
                PricePerNight = p.PricePerNight,
                Latitude = p?.Latitude ?? 0,
                Longitude = p?.Longitude ?? 0,
                MainImageUrl = p.Images.FirstOrDefault()?.ImageUrl,
                AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : null,
                ReviewCount = p.Reviews.Count
            }).ToList();

            return new MapSearchResponseDto
            {
                Properties = propertyDtos,
                TotalCount = propertyDtos.Count
            };
        }

        public async Task<PropertyMapDto?> GetPropertyForMapAsync(int propertyId)
        {
            var property = await _mapRepo.GetPropertyWithLocationAsync(propertyId);
            if (property == null)
                return null;

            return new PropertyMapDto
            {
                Id = property.Id,
                Title = property.Title,
                PricePerNight = property.PricePerNight,
                Latitude = property?.Latitude ?? 0,
                Longitude = property?.Longitude ?? 0,
                MainImageUrl = property.Images.FirstOrDefault()?.ImageUrl,

                AverageRating = property.Reviews.Any() ? property.Reviews.Average(r => r.Rating) : null,
                ReviewCount = property.Reviews.Count
            };
        }
    }
}

