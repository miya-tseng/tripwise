using Microsoft.AspNetCore.Mvc;
using TripWise.Application.Dtos;
using TripWise.Application.Places;
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
        [FromQuery] PlaceSearchKind kind,
        CancellationToken cancellationToken)
    {
        return await placeService.AutocompleteAsync(query, sessionToken, lat, lng, kind, cancellationToken);
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
