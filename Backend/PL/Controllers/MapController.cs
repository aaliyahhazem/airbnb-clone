using BLL.ModelVM.MapsVM;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PL.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class MapController : ControllerBase
    {
        private readonly IMapService _mapService;

        public MapController(IMapService mapService)
        {
            _mapService = mapService;
        }

        // Use Case 1: Guest searches properties by map bounds

        [HttpGet("properties")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchPropertiesOnMap([FromQuery] MapSearchRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.NorthEastLat <= request.SouthWestLat ||
                request.NorthEastLng <= request.SouthWestLng)
            {
                return BadRequest(new { Message = "Invalid map bounds." });
            }

            try
            {
                var result = await _mapService.SearchPropertiesOnMapAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Search failed: {ex.Message}" });
            }
        }

        // Get property details for map popup (Guest)

        [HttpGet("properties/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPropertyForMap(int id)
        {
            try
            {
                var property = await _mapService.GetPropertyForMapAsync(id);
                if (property == null)
                    return NotFound(new { Message = "Property not found." });

                return Ok(property);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Failed to get property: {ex.Message}" });
            }
        }


        // Use Case 2: Host geocodes address when creating property

        [HttpPost("geocode")]
        //[Authorize(Roles ="Host")]
        public async Task<IActionResult> GeocodeAddress([FromBody] GeocodeRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Address))
                return BadRequest(new { Message = "Address is required." });

            try
            {
                var result = await _mapService.GeocodeAddressAsync(request.Address);
                if (result == null)
                    return NotFound(new { Message = "Could not geocode the provided address." });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Geocoding failed: {ex.Message}" });
            }
        }

    }
}



