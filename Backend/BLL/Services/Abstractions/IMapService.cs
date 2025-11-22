using BLL.ModelVM.MapsVM;


namespace BLL.Services.Abstractions
{

        public interface IMapService
        {
            Task<GeocodeResponseDto?> GeocodeAddressAsync(string address);
            Task<MapSearchResponseDto> SearchPropertiesOnMapAsync(MapSearchRequestDto request);
            Task<PropertyMapDto?> GetPropertyForMapAsync(int propertyId);
        }

    
}
