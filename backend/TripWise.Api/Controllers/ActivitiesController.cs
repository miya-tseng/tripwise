using Microsoft.AspNetCore.Mvc;
using TripWise.Application.Dtos;
using TripWise.Application.Services;

namespace TripWise.Api.Controllers;

[ApiController]
[Route("api/trips/{tripId:guid}")]
public class ActivitiesController(ActivityService activityService, SuggestionService suggestionService) : ControllerBase
{
    [HttpPost("activities")]
    public async Task<ActionResult<ActivityDto>> Add(Guid tripId, SaveActivityDto dto, CancellationToken cancellationToken)
    {
        var activity = await activityService.AddAsync(tripId, dto, cancellationToken);

        return CreatedAtAction(nameof(GetSchedule), new { tripId }, activity);
    }

    [HttpPut("activities/{id:guid}")]
    public async Task<ActionResult<ActivityDto>> Update(Guid tripId, Guid id, SaveActivityDto dto, CancellationToken cancellationToken)
    {
        return await activityService.UpdateAsync(tripId, id, dto, cancellationToken);
    }

    [HttpPatch("activities/{id:guid}/move")]
    public async Task<ActionResult<ActivityDto>> Move(
        Guid tripId,
        Guid id,
        MoveActivityDto dto,
        CancellationToken cancellationToken)
    {
        return await activityService.MoveAsync(tripId, id, dto, cancellationToken);
    }

    [HttpDelete("activities/{id:guid}")]
    public async Task<IActionResult> Delete(Guid tripId, Guid id, CancellationToken cancellationToken)
    {
        await activityService.DeleteAsync(tripId, id, cancellationToken);

        return NoContent();
    }

    [HttpGet("activities/{id:guid}/suggestions")]
    public async Task<ActionResult<IReadOnlyList<SuggestionDto>>> GetSuggestions(
        Guid tripId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var suggestions = await suggestionService.GetSuggestionsAsync(tripId, id, cancellationToken);

        return Ok(suggestions);
    }

    [HttpGet("schedule")]
    public async Task<ActionResult<ScheduleDto>> GetSchedule(Guid tripId, CancellationToken cancellationToken)
    {
        return await activityService.GetScheduleAsync(tripId, cancellationToken);
    }
}
