using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TripWise.Application.Currencies;
using TripWise.Domain;
using TripWise.Infrastructure.Currencies;
using TripWise.Infrastructure.Data;
using TripWise.Infrastructure.Data.Repositories;

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
}
