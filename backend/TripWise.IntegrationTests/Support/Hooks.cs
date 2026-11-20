using Reqnroll;
using Reqnroll.BoDi;

namespace TripWise.IntegrationTests.Support;

[Binding]
public class Hooks(IObjectContainer container)
{
    private static TripWiseApiFactory _factory = null!;

    [BeforeTestRun]
    public static async Task StartApi()
    {
        _factory = new TripWiseApiFactory();
        await _factory.InitializeAsync();
    }

    [AfterTestRun]
    public static async Task StopApi()
    {
        await _factory.DisposeAsync();
    }

    [BeforeScenario]
    public void RegisterHttpClient()
    {
        container.RegisterInstanceAs(_factory.CreateClient(), dispose: true);
    }
}
