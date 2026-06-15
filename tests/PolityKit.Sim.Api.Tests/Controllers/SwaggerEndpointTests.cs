using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PolityKit.Sim.Api.Tests.Controllers;

public sealed class SwaggerEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task SwaggerUiIsAvailableInDevelopment()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");
        var content = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("/openapi/v1.json", content);
    }

    [Fact]
    public async Task OpenApiDocumentUsesSwaggerUiCompatibleVersion()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);

        response.EnsureSuccessStatusCode();
        Assert.True(document.RootElement.TryGetProperty("openapi", out var version));
        Assert.StartsWith("3.0.", version.GetString());
    }
}
