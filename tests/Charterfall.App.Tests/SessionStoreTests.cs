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
        await Task.CompletedTask;

        var run = new Charterfall.App.Models.PrototypeRunRecord(
            "pending-original-run",
            "Original",
            content.Crisis.PolityKitScenario,
            content.Crisis.Seed,
            content.Crisis.Ticks,
            IsAuthoritative: false,
            "Integration pending");
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

    [Fact]
    public void CompleteRunResolution_PersistsSuccessfulRunIdAndRequest()
    {
        var store = new InMemoryCharterfallSessionStore();
        var request = new Charterfall.App.Models.CreateRunInput(
            "village-food-crisis",
            12345,
            120,
            ["need-based-allocation"],
            new Dictionary<string, double> { ["needPriorityWeight"] = 1.0 });
        var runId = Guid.NewGuid();
        var result = Charterfall.App.Models.CreateRunResult.Success(
            runId,
            DateTimeOffset.Parse("2026-06-17T10:00:00+00:00"),
            "Village Food Crisis",
            12345,
            120,
            ["Need-Based Allocation"],
            "{\"scenario\":\"village-food-crisis\"}");
        var runRecord = new Charterfall.App.Models.PrototypeRunRecord(
            runId.ToString(),
            "Original",
            "Village Food Crisis",
            12345,
            120,
            IsAuthoritative: true,
            "Created by PolityKit");

        store.BeginRunResolution(request, result.RawRequest);
        store.CompleteRunResolution(request, result, runRecord);

        Assert.Equal(runId.ToString(), store.Current.CurrentRunId);
        Assert.Contains(runId.ToString(), store.Current.AuthoritativeRunIds);
        Assert.Single(store.Current.RunHistory);
        Assert.Equal(request, store.Current.LastSubmittedRunRequest);
        Assert.False(store.Current.IsResolvingRun);
        Assert.Null(store.Current.LastRunError);
    }

    [Fact]
    public void FailRunResolution_KeepsSelectedClausesForRetry()
    {
        var store = new InMemoryCharterfallSessionStore();
        var before = store.Current.SelectedClauseIds.ToArray();
        var request = new Charterfall.App.Models.CreateRunInput(
            "village-food-crisis",
            12345,
            120,
            ["need-based-allocation"],
            new Dictionary<string, double>());

        store.FailRunResolution(request, "{}", "PolityKit API is unavailable.");

        Assert.Equal(before, store.Current.SelectedClauseIds);
        Assert.Empty(store.Current.AuthoritativeRunIds);
        Assert.Empty(store.Current.RunHistory);
        Assert.Equal("PolityKit API is unavailable.", store.Current.LastRunError);
        Assert.False(store.Current.IsResolvingRun);
    }
}
