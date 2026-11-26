using Microsoft.AspNetCore.Mvc;
using TripWise.Application.Dtos;
using TripWise.Application.Services;

namespace TripWise.Api.Controllers;

[ApiController]
[Route("api/places")]
public class PlacesController(PlaceService placeService) : ControllerBase
{
    [HttpGet("autocomplete")]
    public async Task<IReadOnlyList<PlaceSuggestionDto>> Autocomplete(
        [FromQuery] string? query,
        [FromQuery] string? sessionToken,
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        CancellationToken cancellationToken)
    {
        return await placeService.AutocompleteAsync(query, sessionToken, lat, lng, cancellationToken);
    }

    [HttpGet("{placeId}")]
    public async Task<ActionResult<PlaceDto>> GetDetails(
        string placeId,
        [FromQuery] string? sessionToken,
        CancellationToken cancellationToken)
    {
        var place = await placeService.GetDetailsAsync(placeId, sessionToken, cancellationToken);

        return place is null ? NotFound() : place;
    }
}
