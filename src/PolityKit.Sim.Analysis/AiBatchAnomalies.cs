using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolityKit.Sim.Analysis;

public sealed record AiBatchAnomalyReport(
    IReadOnlyList<AiBatchAnomaly> Anomalies);

public sealed record AiBatchAnomaly(
    Guid? RunId,
    string Model,
    string Scenario,
    int Seed,
    string Metric,
    double ObservedValue,
    string Explanation);

public sealed record AiBatchAnomalyValidation(
    bool IsValid,
    IReadOnlyList<string> Errors);

public sealed record AiBatchAnomalyArtifact(
    AiAnalysisArtifact Analysis,
    AiBatchAnomalyReport? Report,
    AiBatchAnomalyValidation Validation)
{
    public bool CanUse => Validation.IsValid && Report is not null;
}

public static class AiBatchAnomalyReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AiBatchAnomalyReport? ReadReport(AiAnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.SuggestedArtifact switch
        {
            null => null,
            AiBatchAnomalyReport report => report,
            IReadOnlyList<AiBatchAnomaly> anomalies => new AiBatchAnomalyReport(anomalies),
            JsonElement json => ReadJsonElement(json),
            _ => null
        };
    }

    public static AiBatchAnomalyValidation Validate(
        AiBatchAnomalyReport? report,
        AiAnalysisProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(provenance);
        if (report is null)
        {
            return new AiBatchAnomalyValidation(false, ["Provider did not return a batch anomaly report."]);
        }

        var errors = new List<string>();
        var sourceRunIds = provenance.SourceRunIds.ToHashSet();
        var models = provenance.ModelNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var scenarios = provenance.ScenarioNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seeds = provenance.Seeds.ToHashSet();
        var metrics = provenance.MetricNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < report.Anomalies.Count; index++)
        {
            var anomaly = report.Anomalies[index];
            var label = $"Anomaly {index + 1}";

            if (anomaly.RunId is not null && !sourceRunIds.Contains(anomaly.RunId.Value))
            {
                errors.Add($"{label} references unknown run ID '{anomaly.RunId}'.");
            }

            if (string.IsNullOrWhiteSpace(anomaly.Model) || !models.Contains(anomaly.Model))
            {
                errors.Add($"{label} references unknown model '{anomaly.Model}'.");
            }

            if (string.IsNullOrWhiteSpace(anomaly.Scenario) || !scenarios.Contains(anomaly.Scenario))
            {
                errors.Add($"{label} references unknown scenario '{anomaly.Scenario}'.");
            }

            if (!seeds.Contains(anomaly.Seed))
            {
                errors.Add($"{label} references unknown seed '{anomaly.Seed}'.");
            }

            if (string.IsNullOrWhiteSpace(anomaly.Metric) || !metrics.Contains(anomaly.Metric))
            {
                errors.Add($"{label} references unknown metric '{anomaly.Metric}'.");
            }

            if (string.IsNullOrWhiteSpace(anomaly.Explanation))
            {
                errors.Add($"{label} must include an explanation.");
            }
        }

        return new AiBatchAnomalyValidation(errors.Count == 0, errors);
    }

    private static AiBatchAnomalyReport? ReadJsonElement(JsonElement json)
    {
        if (json.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        try
        {
            return json.ValueKind == JsonValueKind.Array
                ? new AiBatchAnomalyReport(json.Deserialize<IReadOnlyList<AiBatchAnomaly>>(JsonOptions) ?? [])
                : json.Deserialize<AiBatchAnomalyReport>(JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
