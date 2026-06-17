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
}
