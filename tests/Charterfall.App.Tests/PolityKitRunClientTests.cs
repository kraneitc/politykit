using System.Net;
using System.Text;
using Charterfall.App.Models;
using Charterfall.App.Services;

namespace Charterfall.App.Tests;

public sealed class PolityKitRunClientTests
{
    [Fact]
    public async Task CreateRunAsync_SubmitsExpectedRequestAndReturnsRunDetails()
    {
        string? submittedJson = null;
        var client = new HttpPolityKitRunClient(new HttpClient(new StubHandler(async request =>
        {
            submittedJson = await request.Content!.ReadAsStringAsync();
            var response = """
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "createdAt": "2026-06-17T10:00:00+00:00",
              "scenarioName": "Village Food Crisis",
              "seed": 12345,
              "ticks": 120,
              "models": [
                { "modelName": "Need-Based Allocation" }
              ]
            }
            """;
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            };
        }))
        {
            BaseAddress = new Uri("http://localhost:5020")
        });

        var result = await client.CreateRunAsync(new CreateRunInput(
            "village-food-crisis",
            12345,
            120,
            ["need-based-allocation"],
            new Dictionary<string, double>
            {
                ["needPriorityWeight"] = 1.0,
                ["vulnerabilityPriorityWeight"] = 0.5
            }));

        Assert.True(result.IsSuccess);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), result.Id);
        Assert.Contains("\"scenario\": \"village-food-crisis\"", submittedJson);
        Assert.Contains("\"need-based-allocation\"", submittedJson);
        Assert.DoesNotContain("gameLayerOnly", submittedJson);
    }

    [Fact]
    public async Task CreateRunAsync_ReturnsProblemDetailsAsPlayerReadableFailure()
    {
        var client = new HttpPolityKitRunClient(new HttpClient(new StubHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    "{\"title\":\"Run request is invalid.\",\"detail\":\"Scenario not found.\"}",
                    Encoding.UTF8,
                    "application/json")
            })))
        {
            BaseAddress = new Uri("http://localhost:5020")
        });

        var result = await client.CreateRunAsync(new CreateRunInput(
            "missing-scenario",
            12345,
            120,
            ["need-based-allocation"],
            new Dictionary<string, double>()));

        Assert.False(result.IsSuccess);
        Assert.Equal("Scenario not found.", result.ErrorMessage);
        Assert.Contains("\"scenario\": \"missing-scenario\"", result.RawRequest);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request);
    }
}
