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
}
