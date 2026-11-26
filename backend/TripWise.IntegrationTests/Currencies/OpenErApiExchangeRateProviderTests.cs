using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TripWise.Infrastructure.Currencies;

namespace TripWise.IntegrationTests.Currencies;

public class OpenErApiExchangeRateProviderTests
{
    private const string SuccessBody = """
        {
          "result": "success",
          "time_last_update_unix": 1795564951,
          "base_code": "TWD",
          "rates": { "TWD": 1, "JPY": 4.832835, "USD": 0.03036 }
        }
        """;

    private StubHandler _handler = null!;
    private MemoryCache _cache = null!;
    private OpenErApiExchangeRateProvider _provider = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = new StubHandler();
        _cache = new MemoryCache(new MemoryCacheOptions());
        var options = new ExchangeRateOptions();
        var httpClient = new HttpClient(_handler) { BaseAddress = options.BaseUrl };
        _provider = new OpenErApiExchangeRateProvider(
            httpClient,
            _cache,
            Options.Create(options),
            NullLogger<OpenErApiExchangeRateProvider>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _cache.Dispose();
        _handler.Dispose();
    }

    [Test]
    public async Task GetLatestAsync_RequestsRatesForTwd()
    {
        _handler.Respond(HttpStatusCode.OK, SuccessBody);

        await _provider.GetLatestAsync();

        Assert.That(_handler.RequestedUris.Single().ToString(), Is.EqualTo("https://open.er-api.com/v6/latest/TWD"));
    }

    [Test]
    public async Task GetLatestAsync_ParsesRatesAndUpdateTime()
    {
        _handler.Respond(HttpStatusCode.OK, SuccessBody);

        var rates = await _provider.GetLatestAsync();

        Assert.Multiple(() =>
        {
            Assert.That(rates!.UpdatedAt, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(1795564951)));
            Assert.That(rates.UnitsPerHomeCurrency["JPY"], Is.EqualTo(4.832835m));
            Assert.That(rates.Currencies, Is.EqualTo(new[] { "JPY", "TWD", "USD" }));
        });
    }

    [Test]
    public async Task GetLatestAsync_CachesSuccessfulResponses()
    {
        _handler.Respond(HttpStatusCode.OK, SuccessBody);

        await _provider.GetLatestAsync();
        await _provider.GetLatestAsync();

        Assert.That(_handler.RequestedUris, Has.Count.EqualTo(1));
    }

    [TestCase(HttpStatusCode.InternalServerError, "oops")]
    [TestCase(HttpStatusCode.OK, """{ "result": "error", "error-type": "unsupported-code" }""")]
    [TestCase(HttpStatusCode.OK, "not json")]
    public async Task GetLatestAsync_WhenTheApiFails_ReturnsNull(HttpStatusCode status, string body)
    {
        _handler.Respond(status, body);

        Assert.That(await _provider.GetLatestAsync(), Is.Null);
    }

    [Test]
    public async Task GetLatestAsync_WhenTheApiCannotBeReached_ReturnsNull()
    {
        _handler.Throw(new HttpRequestException("connection refused"));

        Assert.That(await _provider.GetLatestAsync(), Is.Null);
    }

    [Test]
    public async Task GetLatestAsync_CachesFailuresBriefly()
    {
        _handler.Respond(HttpStatusCode.InternalServerError, "oops");

        await _provider.GetLatestAsync();
        await _provider.GetLatestAsync();

        Assert.That(_handler.RequestedUris, Has.Count.EqualTo(1));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private Func<HttpResponseMessage> _respond = () => new HttpResponseMessage(HttpStatusCode.NotFound);

        public List<Uri> RequestedUris { get; } = [];

        public void Respond(HttpStatusCode status, string body) =>
            _respond = () => new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

        public void Throw(Exception exception) => _respond = () => throw exception;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedUris.Add(request.RequestUri!);
            return Task.FromResult(_respond());
        }
    }
}
