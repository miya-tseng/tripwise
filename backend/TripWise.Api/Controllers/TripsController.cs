using Microsoft.AspNetCore.Mvc;
using TripWise.Application.Dtos;
using TripWise.Application.Services;

namespace TripWise.Api.Controllers;

[ApiController]
[Route("api/trips")]
public class TripsController(TripService tripService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TripDto>> Create(CreateTripDto dto, CancellationToken cancellationToken)
    {
        var trip = await tripService.CreateAsync(dto, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = trip.Id }, trip);
    }

    [HttpGet]
    public async Task<IReadOnlyList<TripDto>> GetAll(CancellationToken cancellationToken)
    {
        return await tripService.GetAllAsync(cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TripDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var trip = await tripService.GetByIdAsync(id, cancellationToken);

        return trip is null ? NotFound() : trip;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await tripService.DeleteAsync(id, cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}
