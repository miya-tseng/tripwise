using Microsoft.AspNetCore.Mvc;
using TripWise.Application.Dtos;
using TripWise.Application.Services;

namespace TripWise.Api.Controllers;

[ApiController]
[Route("api/trips/{tripId:guid}/overview")]
public class OverviewController(OverviewService overviewService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TripOverviewDto>> Get(Guid tripId, CancellationToken cancellationToken)
    {
        var overview = await overviewService.GetAsync(tripId, cancellationToken);

        return overview is null ? NotFound() : overview;
    }
}
