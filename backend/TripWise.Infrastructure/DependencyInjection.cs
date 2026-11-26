using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripWise.Application.Currencies;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TripWise.Application.Places;
using TripWise.Application.Suggestions;
using TripWise.Domain;
using TripWise.Infrastructure.Currencies;
using TripWise.Infrastructure.Data;
using TripWise.Infrastructure.Data.Repositories;
using TripWise.Infrastructure.Places;
using TripWise.Infrastructure.Suggestions;

namespace TripWise.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Default";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString(ConnectionStringName)));

        services.AddScoped<ITripRepository, TripRepository>();

        AddExchangeRates(services, configuration);
        AddPlaceSearch(services, configuration);
        AddSuggestions(services, configuration);

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>();

        return services;
    }

    private static void AddExchangeRates(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ExchangeRateOptions.SectionName);
        services.Configure<ExchangeRateOptions>(section);
        var exchangeRateOptions = section.Get<ExchangeRateOptions>() ?? new ExchangeRateOptions();

        if (exchangeRateOptions.Provider == ExchangeRateOptions.FakeProvider)
        {
            services.AddSingleton<IExchangeRateProvider, FakeExchangeRateProvider>();
            return;
        }

        services.AddMemoryCache();
        services.AddHttpClient<IExchangeRateProvider, OpenErApiExchangeRateProvider>(client =>
        {
            client.BaseAddress = exchangeRateOptions.BaseUrl;
            client.Timeout = exchangeRateOptions.Timeout;
        });
    }

    private static void AddPlaceSearch(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(GooglePlacesOptions.SectionName);
        services.Configure<GooglePlacesOptions>(section);
        var placesOptions = section.Get<GooglePlacesOptions>() ?? new GooglePlacesOptions();

        if (!placesOptions.UsesGoogle)
        {
            services.AddSingleton<IPlaceSearch, FakePlaceSearch>();
            return;
        }

        services.AddHttpClient<IPlaceSearch, GooglePlaceSearch>(client =>
        {
            client.BaseAddress = placesOptions.BaseUrl;
            client.Timeout = placesOptions.Timeout;
        });
    }

    private static void AddSuggestions(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(GeminiOptions.SectionName);
        services.Configure<GeminiOptions>(section);
        var geminiOptions = section.Get<GeminiOptions>() ?? new GeminiOptions();

        if (!geminiOptions.UsesGemini)
        {
            services.AddSingleton<ISuggestionGenerator, FakeSuggestionGenerator>();
            return;
        }

        services.AddMemoryCache();
        services.AddHttpClient<GeminiSuggestionGenerator>(client =>
        {
            client.BaseAddress = geminiOptions.BaseUrl;
            client.Timeout = geminiOptions.Timeout;
        });
        services.AddScoped<ISuggestionGenerator>(provider => new CachedSuggestionGenerator(
            provider.GetRequiredService<GeminiSuggestionGenerator>(),
            provider.GetRequiredService<IMemoryCache>(),
            provider.GetRequiredService<IOptions<GeminiOptions>>()));
    }
}
