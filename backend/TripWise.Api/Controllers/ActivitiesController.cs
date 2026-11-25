using Microsoft.AspNetCore.Mvc;
using TripWise.Api.Dtos;
using TripWise.Api.Services;

namespace TripWise.Api.Controllers;

[ApiController]
[Route("api/trips/{tripId:guid}")]
public class ActivitiesController(ActivityService activityService) : ControllerBase
{
    [HttpPost("activities")]
    public async Task<ActionResult<ActivityDto>> Add(Guid tripId, SaveActivityDto dto, CancellationToken cancellationToken)
    {
        var activity = await activityService.AddAsync(tripId, dto, cancellationToken);

        return activity is null
            ? NotFound()
            : CreatedAtAction(nameof(GetSchedule), new { tripId }, activity);
    }

    [HttpPut("activities/{id:guid}")]
    public async Task<ActionResult<ActivityDto>> Update(Guid tripId, Guid id, SaveActivityDto dto, CancellationToken cancellationToken)
    {
        var activity = await activityService.UpdateAsync(tripId, id, dto, cancellationToken);

        return activity is null ? NotFound() : activity;
    }

    [HttpDelete("activities/{id:guid}")]
    public async Task<IActionResult> Delete(Guid tripId, Guid id, CancellationToken cancellationToken)
    {
        var deleted = await activityService.DeleteAsync(tripId, id, cancellationToken);

        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("schedule")]
    public async Task<ActionResult<ScheduleDto>> GetSchedule(Guid tripId, CancellationToken cancellationToken)
    {
        var schedule = await activityService.GetScheduleAsync(tripId, cancellationToken);

        return schedule is null ? NotFound() : schedule;
    }
}
