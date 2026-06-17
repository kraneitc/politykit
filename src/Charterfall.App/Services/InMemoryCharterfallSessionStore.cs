using Charterfall.App.Models;

namespace Charterfall.App.Services;

public sealed class InMemoryCharterfallSessionStore : ICharterfallSessionStore
{
    public CharterfallSession Current { get; } = new();

    public void ClearError()
    {
        Current.LastError = null;
    }

    public void SetError(string message)
    {
        Current.LastError = message;
    }

    public void SelectCrisis(CrisisCard crisis)
    {
        Current.ChapterNumber = crisis.ChapterNumber;
        Current.ActiveCrisisId = crisis.Id;
        Current.ScenarioSource = crisis.PolityKitScenario;
        Current.Seed = crisis.Seed;
        Current.Ticks = crisis.Ticks;
        Current.AssumptionsSummary =
            $"Greywater Compact / {crisis.DisplayName} / {crisis.PolityKitScenario} / seed {crisis.Seed} / {crisis.Ticks} ticks";
    }

    public void UpdateClauseSelection(
        IReadOnlyList<string> selectedClauseIds,
        CharterRunInputPreview preview,
        ClauseSelectionValidationResult validation,
        IReadOnlyList<CharterClauseDefinition> selectedClauses)
    {
        var selectedClauseSnapshot = selectedClauseIds.ToArray();
        var modelSnapshot = preview.Models.ToArray();
        var gameLayerSnapshot = preview.GameLayerOnlyClauses.ToArray();
        var errorSnapshot = validation.Errors.ToArray();

        Current.SelectedClauseIds.Clear();
        Current.SelectedClauseIds.AddRange(selectedClauseSnapshot);

        Current.AuthoritativeModelIds.Clear();
        Current.AuthoritativeModelIds.AddRange(modelSnapshot);

        Current.AuthoritativeParameters.Clear();
        foreach (var parameter in preview.Parameters)
        {
            Current.AuthoritativeParameters[parameter.Key] = parameter.Value;
        }

        Current.GameLayerClauseIds.Clear();
        Current.GameLayerClauseIds.AddRange(gameLayerSnapshot);

        Current.ClauseSelectionErrors.Clear();
        Current.ClauseSelectionErrors.AddRange(errorSnapshot);

        Current.CharterSummary = selectedClauses.Count == 0
            ? "No charter clauses selected"
            : string.Join(", ", selectedClauses.Select(clause => clause.DisplayName));
    }
}
