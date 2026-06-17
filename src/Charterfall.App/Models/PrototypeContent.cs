namespace Charterfall.App.Models;

public sealed record SettlementProfile(string Id, string Name, string Premise);

public sealed record CrisisCard(
    string Id,
    int ChapterNumber,
    string DisplayName,
    string Summary,
    string PolityKitScenario,
    int Seed,
    int Ticks,
    string DesignPurpose,
    IReadOnlyList<string> StressedClauseDimensions,
    bool IsUnlocked,
    bool IsIntegrationAvailable,
    string IntegrationStatus);

public sealed record CharterClauseDefinition(
    string Id,
    string Dimension,
    string DisplayName,
    string Description,
    string Tradeoff,
    string MappingStatus,
    string? ModelId,
    IReadOnlyDictionary<string, double> Parameters,
    IReadOnlyList<string> GameLayerEffects,
    string Availability,
    string Boundary,
    IReadOnlyList<string> PowerIncentiveTags);

public sealed record CharterRunInputPreview(
    IReadOnlyList<string> Models,
    IReadOnlyDictionary<string, double> Parameters,
    IReadOnlyList<string> GameLayerOnlyClauses,
    IReadOnlyList<string> PresetDimensionClauses);

public sealed record ClauseSelectionValidationResult(bool IsValid, IReadOnlyList<string> Errors);

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
    IReadOnlyList<CrisisCard> CampaignCrises,
    IReadOnlyList<CharterClauseDefinition> Clauses,
    IReadOnlyList<MetricPlaceholder> Metrics,
    string ClaimsBoundary);
