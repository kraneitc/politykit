using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.Simulation;

namespace PolityKit.Sim.Tests.Core;

public sealed class CoreModelDefaultsTests
{
    [Fact]
    public void SystemDecisionStartsWithUsableCollections()
    {
        var decision = new SystemDecision();

        decision.Allocations.Add(new ResourceAllocation());
        decision.PolicyChanges.Add(new PolicyChange());
        decision.InstitutionalActions.Add(new InstitutionalAction());

        Assert.Single(decision.Allocations);
        Assert.Single(decision.PolicyChanges);
        Assert.Single(decision.InstitutionalActions);
    }

    [Fact]
    public void ScenarioDefinitionStartsWithUsableNestedState()
    {
        var scenario = new ScenarioDefinition();

        scenario.Shocks.Add(new ShockDefinition());

        Assert.NotNull(scenario.InitialResources);
        Assert.Single(scenario.Shocks);
    }

    [Fact]
    public void RunConfigurationStartsWithUsableCollections()
    {
        var configuration = new RunConfiguration();

        configuration.ModelNames.Add("NeedBasedAllocation");
        configuration.Parameters["scarcity"] = 0.5;

        Assert.Contains("NeedBasedAllocation", configuration.ModelNames);
        Assert.Equal(0.5, configuration.Parameters["scarcity"]);
    }

    [Fact]
    public void SystemContextDefaultsToDeterministicRandomSource()
    {
        var context = new SystemContext();

        Assert.Equal(0, context.Random.Seed);
        Assert.Empty(context.Parameters);
    }

    [Fact]
    public void ModelManifestStartsWithUsableCollections()
    {
        var manifest = new ModelManifest();

        manifest.Assumptions.Add(new ModelAssumption { Name = "scarcity" });
        manifest.KnownFailureModes.Add("Administrative overload");

        Assert.Single(manifest.Assumptions);
        Assert.Single(manifest.KnownFailureModes);
    }
}
