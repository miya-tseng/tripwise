using Microsoft.AspNetCore.Mvc;
using TripWise.Application.Dtos;
using TripWise.Application.Services;

namespace TripWise.Api.Controllers;

[ApiController]
[Route("api/trips/{tripId:guid}/stays")]
public class StaysController(StayService stayService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<StayDto>> Add(Guid tripId, SaveStayDto dto, CancellationToken cancellationToken)
    {
        var stay = await stayService.AddAsync(tripId, dto, cancellationToken);

        return CreatedAtAction(nameof(ActivitiesController.GetSchedule), "Activities", new { tripId }, stay);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StayDto>> Update(Guid tripId, Guid id, SaveStayDto dto, CancellationToken cancellationToken)
    {
        return await stayService.UpdateAsync(tripId, id, dto, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid tripId, Guid id, CancellationToken cancellationToken)
    {
        await stayService.DeleteAsync(tripId, id, cancellationToken);

        return NoContent();
    }
}
