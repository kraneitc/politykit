using System.Net.Http.Json;
using System.Text.Json;
using Charterfall.App.Models;

namespace Charterfall.App.Services;

public sealed class HttpPolityKitRunClient(HttpClient httpClient) : IPolityKitRunClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<CreateRunResult> CreateRunAsync(
        CreateRunInput input,
        CancellationToken cancellationToken = default)
    {
        var rawRequest = JsonSerializer.Serialize(ToRequest(input), JsonOptions);

        try
        {
            using var response = await httpClient.PostAsJsonAsync("api/runs", ToRequest(input), JsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return CreateRunResult.Failure(rawRequest, await ReadErrorAsync(response, cancellationToken));
            }

            var run = await response.Content.ReadFromJsonAsync<RunDetailDto>(JsonOptions, cancellationToken);
            if (run is null)
            {
                return CreateRunResult.Failure(rawRequest, "PolityKit returned an empty run response.");
            }

            return CreateRunResult.Success(
                run.Id,
                run.CreatedAt,
                run.ScenarioName,
                run.Seed,
                run.Ticks,
                run.Models.Select(model => model.ModelName).ToArray(),
                rawRequest);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateRunResult.Failure(rawRequest, "PolityKit API did not respond in time. Check that the API is running and try again.");
        }
        catch (HttpRequestException)
        {
            return CreateRunResult.Failure(rawRequest, "PolityKit API is unavailable. Start the API and try resolving again.");
        }
    }

    private static CreateRunRequestDto ToRequest(CreateRunInput input) =>
        new(input.Scenario, input.Seed, input.Ticks, input.Models, input.Parameters);

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDto>(JsonOptions, cancellationToken);
        if (problem is not null && !string.IsNullOrWhiteSpace(problem.Detail))
        {
            return problem.Detail;
        }

        if (problem is not null && !string.IsNullOrWhiteSpace(problem.Title))
        {
            return problem.Title;
        }

        return $"PolityKit rejected the run request with HTTP {(int)response.StatusCode}.";
    }

    private sealed record CreateRunRequestDto(
        string Scenario,
        int Seed,
        int Ticks,
        IReadOnlyList<string> Models,
        IReadOnlyDictionary<string, double> Parameters);

    private sealed class RunDetailDto
    {
        public Guid Id { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public string ScenarioName { get; init; } = "";

        public int Seed { get; init; }

        public int Ticks { get; init; }

        public IReadOnlyList<ModelRunSummaryDto> Models { get; init; } = [];
    }

    private sealed class ModelRunSummaryDto
    {
        public string ModelName { get; init; } = "";
    }

    private sealed class ProblemDetailsDto
    {
        public string? Title { get; init; }

        public string? Detail { get; init; }
    }
}
