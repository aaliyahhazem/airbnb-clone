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
        private readonly ILogger<MapController> _logger;

        public MapController(IMapService mapService, ILogger<MapController> logger)
        {
            _mapService = mapService;
            _logger = logger;
        }

        // Use Case 1: Guest searches properties by map bounds

        [HttpGet("properties")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchPropertiesOnMap([FromQuery] MapSearchRequestDto request)
        {
            try
            {
                _logger.LogInformation($"Map search request - NE: ({request.NorthEastLat}, {request.NorthEastLng}), SW: ({request.SouthWestLat}, {request.SouthWestLng})");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning($"Invalid model state: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors))}");
                    return BadRequest(ModelState);
                }

                if (request.NorthEastLat <= request.SouthWestLat ||
                    request.NorthEastLng <= request.SouthWestLng)
                {
                    return BadRequest(new { Message = "Invalid map bounds." });
                }

                var result = await _mapService.SearchPropertiesOnMapAsync(request);
                _logger.LogInformation($"Returned {result.Properties.Count} properties");
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Argument error: {ex.Message}");
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search failed");
                return StatusCode(500, new { Message = $"Search failed: {ex.Message}", Details = ex.StackTrace });
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
                _logger.LogError(ex, "Failed to get property");
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
                _logger.LogError(ex, "Geocoding failed");
                return StatusCode(500, new { Message = $"Geocoding failed: {ex.Message}" });
            }
        }

    }
}

