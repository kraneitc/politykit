using Charterfall.App.Models;

namespace Charterfall.App.Services;

public interface IPrototypeContentProvider
{
    PrototypeContent GetInitialContent();

    IReadOnlyList<CrisisCard> GetCampaignCrises();

    CrisisCard GetActiveCrisis(int chapterNumber);

    CrisisCard? FindCrisis(string crisisId);

    IReadOnlyList<CharterClauseDefinition> GetClauses();

    IReadOnlyList<CharterClauseDefinition> GetClausesForDimension(string dimension);

    CharterClauseDefinition? FindClause(string clauseId);

    CharterRunInputPreview BuildRunInputPreview(IReadOnlyList<string> selectedClauseIds);

    ClauseSelectionValidationResult ValidateClauseSelection(IReadOnlyList<string> selectedClauseIds);
}
