using Microsoft.AspNetCore.Mvc;
using TripWise.Application.Dtos;
using TripWise.Application.Services;

namespace TripWise.Api.Controllers;

[ApiController]
[Route("api/exchange-rates")]
public class ExchangeRatesController(ExchangeRateService exchangeRateService) : ControllerBase
{
    [HttpGet]
    public async Task<ExchangeRatesDto> GetLatest(CancellationToken cancellationToken)
    {
        return await exchangeRateService.GetLatestAsync(cancellationToken);
    }
}
