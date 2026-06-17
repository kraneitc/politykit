using Charterfall.App.Models;

namespace Charterfall.App.Services;

public interface ICharterfallSessionStore
{
    CharterfallSession Current { get; }

    void ClearError();

    void SetError(string message);

    void SelectCrisis(CrisisCard crisis);

    void UpdateClauseSelection(
        IReadOnlyList<string> selectedClauseIds,
        CharterRunInputPreview preview,
        ClauseSelectionValidationResult validation,
        IReadOnlyList<CharterClauseDefinition> selectedClauses);

    void BeginRunResolution(CreateRunInput request, string requestJson);

    void CompleteRunResolution(CreateRunInput request, CreateRunResult result, PrototypeRunRecord runRecord);

    void FailRunResolution(CreateRunInput request, string requestJson, string errorMessage);
}
