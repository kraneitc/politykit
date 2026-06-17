using Charterfall.App.Models;

namespace Charterfall.App.Services;

public interface ICharterfallSessionStore
{
    CharterfallSession Current { get; }

    void ClearError();

    void SetError(string message);
}
