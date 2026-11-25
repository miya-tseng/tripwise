using System.Net;
using Reqnroll;
using TripWise.IntegrationTests.Support;

namespace TripWise.IntegrationTests.StepDefinitions;

[Binding]
public class CommonStepDefinitions(ApiResponse response)
{
    [Then("the response status is {int}")]
    public void ThenTheResponseStatusIs(int statusCode)
    {
        Assert.That(response.Message.StatusCode, Is.EqualTo((HttpStatusCode)statusCode));
    }

    [Then("the error detail is {string}")]
    public async Task ThenTheErrorDetailIs(string detail)
    {
        var body = await response.Message.Content.ReadAsStringAsync();

        Assert.That(body, Does.Contain(detail));
    }
}
