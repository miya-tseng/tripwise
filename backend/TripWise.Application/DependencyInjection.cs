using Microsoft.Extensions.DependencyInjection;
using TripWise.Application.Services;

namespace TripWise.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<TripService>();
        services.AddScoped<ActivityService>();
        services.AddScoped<ExchangeRateService>();
        services.AddScoped<PlaceService>();
        services.AddScoped<SuggestionService>();

        return services;
    }
}
