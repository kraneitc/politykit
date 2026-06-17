namespace Charterfall.App.Models;

public sealed record CreateRunInput(
    string Scenario,
    int Seed,
    int Ticks,
    IReadOnlyList<string> Models,
    IReadOnlyDictionary<string, double> Parameters);

public sealed record CreateRunResult(
    bool IsSuccess,
    Guid? Id,
    DateTimeOffset? CreatedAt,
    string ScenarioName,
    int Seed,
    int Ticks,
    IReadOnlyList<string> Models,
    string RawRequest,
    string? ErrorMessage)
{
    public static CreateRunResult Success(
        Guid id,
        DateTimeOffset createdAt,
        string scenarioName,
        int seed,
        int ticks,
        IReadOnlyList<string> models,
        string rawRequest) =>
        new(true, id, createdAt, scenarioName, seed, ticks, models, rawRequest, null);

    public static CreateRunResult Failure(string rawRequest, string errorMessage) =>
        new(false, null, null, string.Empty, 0, 0, [], rawRequest, errorMessage);
}
