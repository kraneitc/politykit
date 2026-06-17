using Charterfall.App.Models;

namespace Charterfall.App.Services;

public interface IPrototypeContentProvider
{
    PrototypeContent GetInitialContent();

    IReadOnlyList<CrisisCard> GetCampaignCrises();

    CrisisCard GetActiveCrisis(int chapterNumber);

    CrisisCard? FindCrisis(string crisisId);
}
