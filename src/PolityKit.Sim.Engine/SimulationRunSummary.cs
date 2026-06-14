using PolityKit.Sim.Core.Events;
using PolityKit.Sim.Core.Metrics;

namespace PolityKit.Sim.Engine;

public sealed class SimulationRunSummary
{
    private const int NearbyEventWindow = 2;
    private const int MetricChangeWindow = 5;
    private const int MaxNearbyEvents = 5;

    public string ScenarioName { get; init; } = "";

    public int Seed { get; init; }

    public int Ticks { get; init; }

    public IReadOnlyList<ModelRunSummary> Models { get; init; } = [];

    public static SimulationRunSummary Create(SimulationRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new SimulationRunSummary
        {
            ScenarioName = result.ScenarioName,
            Seed = result.Seed,
            Ticks = result.Ticks,
            Models = result.ModelResults.Select(CreateModelSummary).ToArray()
        };
    }

    private static ModelRunSummary CreateModelSummary(ModelRunResult model)
    {
        return new ModelRunSummary
        {
            ModelName = model.ModelName,
            ModelVersion = model.ModelVersion,
            EventCount = model.Events.Count,
            EventCountsByType = model.Events
                .GroupBy(simEvent => simEvent.Type)
                .OrderBy(group => group.Key)
                .ToDictionary(group => group.Key, group => group.Count()),
            FinalMetrics = model.Metrics
                .GroupBy(metric => metric.Name)
                .Select(group => group.OrderByDescending(metric => metric.Tick).First())
                .OrderBy(metric => metric.Name)
                .Select(metric => new MetricSummary
                {
                    Name = metric.Name,
                    Tick = metric.Tick,
                    Value = metric.Value,
                    Unit = metric.Unit
                })
                .ToArray(),
            NotableMetricChanges = model.Metrics
                .GroupBy(metric => metric.Name)
                .Select(group => FindNotableChange(group.OrderBy(metric => metric.Tick).ToArray(), model.Events))
                .Where(change => change is not null)
                .Select(change => change!)
                .OrderByDescending(change => change.AbsoluteChange)
                .ThenBy(change => change.Metric)
                .ToArray()
        };
    }

    private static MetricChangeSummary? FindNotableChange(
        IReadOnlyList<MetricResult> metrics,
        IReadOnlyList<SimulationEvent> events)
    {
        if (metrics.Count < 2)
        {
            return null;
        }

        MetricResult? previous = null;
        MetricResult? current = null;
        var largestChange = 0.0;

        for (var index = 1; index < metrics.Count; index++)
        {
            var firstComparisonIndex = Math.Max(0, index - MetricChangeWindow);
            for (var comparisonIndex = firstComparisonIndex; comparisonIndex < index; comparisonIndex++)
            {
                var change = Math.Abs(metrics[index].Value - metrics[comparisonIndex].Value);
                if (change > largestChange)
                {
                    previous = metrics[comparisonIndex];
                    current = metrics[index];
                    largestChange = change;
                }
            }
        }

        if (previous is null || current is null || largestChange < NotableChangeThreshold(current.Name))
        {
            return null;
        }

        var delta = current.Value - previous.Value;
        return new MetricChangeSummary
        {
            Metric = current.Name,
            FromTick = previous.Tick,
            ToTick = current.Tick,
            PreviousValue = previous.Value,
            Value = current.Value,
            Change = delta,
            AbsoluteChange = Math.Abs(delta),
            Direction = delta > 0 ? "increased" : "decreased",
            Unit = current.Unit,
            NearbyEvents = SelectNearbyEvents(events, current.Tick)
        };
    }

    private static double NotableChangeThreshold(string metricName)
    {
        return metricName switch
        {
            "Needs Met" => 0.05,
            "Inequality" => 0.05,
            "Trust" => 3.0,
            "Severe Failures" => 1.0,
            "Administrative Load" => 1.0,
            _ => 1.0
        };
    }

    private static IReadOnlyList<EventSummary> SelectNearbyEvents(
        IReadOnlyList<SimulationEvent> events,
        int tick)
    {
        return events
            .Where(simEvent => Math.Abs(simEvent.Tick - tick) <= NearbyEventWindow)
            .Where(IsInterpretabilityEvent)
            .OrderBy(simEvent => Math.Abs(simEvent.Tick - tick))
            .ThenBy(simEvent => simEvent.Tick)
            .ThenBy(simEvent => simEvent.Type)
            .GroupBy(simEvent => simEvent.Type)
            .Select(group => group.First())
            .Take(MaxNearbyEvents)
            .Select(simEvent => new EventSummary
            {
                Tick = simEvent.Tick,
                Type = simEvent.Type,
                Description = simEvent.Description,
                Data = simEvent.Data
            })
            .ToArray();
    }

    private static bool IsInterpretabilityEvent(SimulationEvent simEvent)
    {
        return simEvent.Type != "ResourceAllocated";
    }
}

public sealed class ModelRunSummary
{
    public string ModelName { get; init; } = "";

    public string ModelVersion { get; init; } = "";

    public int EventCount { get; init; }

    public IReadOnlyDictionary<string, int> EventCountsByType { get; init; } = new Dictionary<string, int>();

    public IReadOnlyList<MetricSummary> FinalMetrics { get; init; } = [];

    public IReadOnlyList<MetricChangeSummary> NotableMetricChanges { get; init; } = [];
}

public sealed class MetricSummary
{
    public string Name { get; init; } = "";

    public int Tick { get; init; }

    public double Value { get; init; }

    public string Unit { get; init; } = "";
}

public sealed class MetricChangeSummary
{
    public string Metric { get; init; } = "";

    public int FromTick { get; init; }

    public int ToTick { get; init; }

    public double PreviousValue { get; init; }

    public double Value { get; init; }

    public double Change { get; init; }

    public double AbsoluteChange { get; init; }

    public string Direction { get; init; } = "";

    public string Unit { get; init; } = "";

    public IReadOnlyList<EventSummary> NearbyEvents { get; init; } = [];
}

public sealed class EventSummary
{
    public int Tick { get; init; }

    public string Type { get; init; } = "";

    public string Description { get; init; } = "";

    public IReadOnlyDictionary<string, object> Data { get; init; } = new Dictionary<string, object>();
}
