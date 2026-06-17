namespace Charterfall.App.Models;

public sealed record SettlementProfile(string Id, string Name, string Premise);

public sealed record CrisisCard(
    string Id,
    string Name,
    string ScenarioId,
    int Seed,
    int Ticks,
    string ModelPreset,
    string IntegrationStatus);

public sealed record CharterClause(string Id, string Name, string Description, bool IsAuthoritativeCandidate);

public sealed record MetricPlaceholder(string Id, string Name);

public sealed record PrototypeRunRecord(
    string RunId,
    string Label,
    string ScenarioId,
    int Seed,
    int Ticks,
    bool IsAuthoritative,
    string Status);

public sealed record PrototypeContent(
    SettlementProfile Settlement,
    CrisisCard Crisis,
    IReadOnlyList<CharterClause> Clauses,
    IReadOnlyList<MetricPlaceholder> Metrics,
    string ClaimsBoundary);
