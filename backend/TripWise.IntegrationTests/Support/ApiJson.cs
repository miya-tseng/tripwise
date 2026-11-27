using System.Text.Json;
using TripWise.Api;

namespace TripWise.IntegrationTests.Support;

public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = ApiJsonOptions.Create();
}
