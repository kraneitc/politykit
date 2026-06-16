using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolityKit.Sim.Analysis;

public sealed record AiModelCritique(
    IReadOnlyList<AiModelCritiqueItem> AssumptionRisks,
    IReadOnlyList<AiModelCritiqueItem> ObservedFailureModes,
    IReadOnlyList<string> SuggestedTests,
    IReadOnlyList<string> SuggestedDocumentationUpdates);

public sealed record AiModelCritiqueItem(
    string Title,
    string Summary,
    IReadOnlyList<string> Evidence);

public sealed record AiModelCritiqueArtifact(
    AiAnalysisArtifact Analysis,
    AiModelCritique? Critique);

public static class AiModelCritiqueReader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AiModelCritique? ReadCritique(AiAnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.SuggestedArtifact switch
        {
            null => null,
            AiModelCritique critique => critique,
            JsonElement json => ReadJsonElement(json),
            _ => null
        };
    }

    private static AiModelCritique? ReadJsonElement(JsonElement json)
    {
        if (json.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        try
        {
            return json.Deserialize<AiModelCritique>(JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
