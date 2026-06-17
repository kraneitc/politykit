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
        Assert.Contains("allocation.need_based", store.Current.SelectedClauseIds);
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
}
