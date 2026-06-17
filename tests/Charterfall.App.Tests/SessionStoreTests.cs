using Charterfall.App.Services;

namespace Charterfall.App.Tests;

public sealed class SessionStoreTests
{
    [Fact]
    public void Current_StartsWithCharterfallOwnedSessionDefaults()
    {
        var store = new InMemoryCharterfallSessionStore();

        Assert.Equal("greywater-compact", store.Current.SettlementId);
        Assert.Equal(1, store.Current.ChapterNumber);
        Assert.Equal("failed-harvest", store.Current.ActiveCrisisId);
        Assert.Equal("village-food-crisis", store.Current.ScenarioSource);
        Assert.Equal(12345, store.Current.Seed);
        Assert.Equal(120, store.Current.Ticks);
        Assert.Contains("Failed Harvest", store.Current.AssumptionsSummary);
        Assert.Contains("allocation.need_based", store.Current.SelectedClauseIds);
        Assert.Contains("need-based-allocation", store.Current.AuthoritativeModelIds);
        Assert.Contains("emergency.none", store.Current.GameLayerClauseIds);
        Assert.Contains("Need-Based Allocation", store.Current.CharterSummary);
        Assert.Empty(store.Current.AuthoritativeRunIds);
        Assert.Null(store.Current.SelectedContinuationRunId);
        Assert.Null(store.Current.LastError);
        Assert.False(store.Current.IsBusy);
    }

    [Fact]
    public async Task Current_CanStoreRunHistoryWithoutTouchingAuthoritativeRunIds()
    {
        var store = new InMemoryCharterfallSessionStore();
        var content = new PrototypeContentProvider().GetInitialContent();
        var client = new PlaceholderPolityKitRunClient();

        var run = await client.CreatePlaceholderRunAsync("Original", content.Crisis);
        store.Current.RunHistory.Add(run);

        Assert.Single(store.Current.RunHistory);
        Assert.Equal("pending-original-run", store.Current.RunHistory[0].RunId);
        Assert.False(store.Current.RunHistory[0].IsAuthoritative);
        Assert.Empty(store.Current.AuthoritativeRunIds);
    }

    [Fact]
    public void SelectCrisis_StoresScenarioFieldsInSessionState()
    {
        var store = new InMemoryCharterfallSessionStore();
        var crisis = new PrototypeContentProvider().GetActiveCrisis(1);

        store.SelectCrisis(crisis);

        Assert.Equal(1, store.Current.ChapterNumber);
        Assert.Equal("failed-harvest", store.Current.ActiveCrisisId);
        Assert.Equal("village-food-crisis", store.Current.ScenarioSource);
        Assert.Equal(12345, store.Current.Seed);
        Assert.Equal(120, store.Current.Ticks);
        Assert.Equal("Greywater Compact / Failed Harvest / village-food-crisis / seed 12345 / 120 ticks", store.Current.AssumptionsSummary);
    }

    [Fact]
    public void UpdateClauseSelection_PersistsSelectedIdsIndependentlyFromDerivedPreview()
    {
        var store = new InMemoryCharterfallSessionStore();
        var provider = new PrototypeContentProvider();
        var selectedIds = new[] { "allocation.market_based", "emergency.limited" };
        var preview = provider.BuildRunInputPreview(selectedIds);
        var validation = provider.ValidateClauseSelection(selectedIds);
        var selectedClauses = selectedIds
            .Select(provider.FindClause)
            .OfType<Charterfall.App.Models.CharterClauseDefinition>()
            .ToArray();

        store.UpdateClauseSelection(selectedIds, preview, validation, selectedClauses);

        Assert.Equal(selectedIds, store.Current.SelectedClauseIds);
        Assert.Equal(["market-based-allocation"], store.Current.AuthoritativeModelIds);
        Assert.Equal(["emergency.limited"], store.Current.GameLayerClauseIds);
        Assert.Contains("Market-Based Allocation", store.Current.CharterSummary);
        Assert.Empty(store.Current.ClauseSelectionErrors);
    }

    [Fact]
    public void UpdateClauseSelection_CanReuseCurrentSelectedClauseListWithoutClearingSelection()
    {
        var store = new InMemoryCharterfallSessionStore();
        var provider = new PrototypeContentProvider();
        var preview = provider.BuildRunInputPreview(store.Current.SelectedClauseIds);
        var validation = provider.ValidateClauseSelection(store.Current.SelectedClauseIds);
        var selectedClauses = store.Current.SelectedClauseIds
            .Select(provider.FindClause)
            .OfType<Charterfall.App.Models.CharterClauseDefinition>()
            .ToArray();

        store.UpdateClauseSelection(store.Current.SelectedClauseIds, preview, validation, selectedClauses);

        Assert.Contains("allocation.need_based", store.Current.SelectedClauseIds);
        Assert.Contains("need-based-allocation", store.Current.AuthoritativeModelIds);
        Assert.Empty(store.Current.ClauseSelectionErrors);
    }
}
