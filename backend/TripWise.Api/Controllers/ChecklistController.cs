using Microsoft.AspNetCore.Mvc;
using TripWise.Application.Dtos;
using TripWise.Application.Services;

namespace TripWise.Api.Controllers;

[ApiController]
[Route("api/trips/{tripId:guid}/checklist")]
public class ChecklistController(ChecklistService checklistService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ChecklistDto>> Get(Guid tripId, CancellationToken cancellationToken) =>
        await checklistService.GetAsync(tripId, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<ChecklistDto>> Add(Guid tripId, SaveChecklistItemDto dto, CancellationToken cancellationToken)
    {
        var checklist = await checklistService.AddAsync(tripId, dto, cancellationToken);

        return CreatedAtAction(nameof(Get), new { tripId }, checklist);
    }

    [HttpPut("{itemId:guid}")]
    public async Task<ActionResult<ChecklistDto>> Update(Guid tripId, Guid itemId, SaveChecklistItemDto dto, CancellationToken cancellationToken) =>
        await checklistService.UpdateAsync(tripId, itemId, dto, cancellationToken);

    [HttpPut("{itemId:guid}/done")]
    public async Task<ActionResult<ChecklistDto>> SetDone(Guid tripId, Guid itemId, SetDoneDto dto, CancellationToken cancellationToken) =>
        await checklistService.SetDoneAsync(tripId, itemId, dto.Done!.Value, cancellationToken);

    [HttpDelete("{itemId:guid}")]
    public async Task<ActionResult<ChecklistDto>> Delete(Guid tripId, Guid itemId, CancellationToken cancellationToken) =>
        await checklistService.DeleteAsync(tripId, itemId, cancellationToken);

    [HttpPut("order")]
    public async Task<ActionResult<ChecklistDto>> Reorder(Guid tripId, ReorderChecklistDto dto, CancellationToken cancellationToken) =>
        await checklistService.ReorderAsync(tripId, dto, cancellationToken);

    [HttpPut("bookings/activities/{activityId:guid}")]
    public async Task<ActionResult<ChecklistDto>> SetActivityBooked(Guid tripId, Guid activityId, SetDoneDto dto, CancellationToken cancellationToken) =>
        await checklistService.SetActivityBookedAsync(tripId, activityId, dto.Done!.Value, cancellationToken);

    [HttpPut("bookings/stays/{stayId:guid}")]
    public async Task<ActionResult<ChecklistDto>> SetStayBooked(Guid tripId, Guid stayId, SetDoneDto dto, CancellationToken cancellationToken) =>
        await checklistService.SetStayBookedAsync(tripId, stayId, dto.Done!.Value, cancellationToken);
}
