using PolityKit.Sim.Core.Scenarios;
using PolityKit.Sim.Core.World;
using PolityKit.Sim.Scenarios;

namespace PolityKit.Sim.Tests.Scenarios;

public sealed class ScenarioResolverTests
{
    [Fact]
    public void ResolveReturnsDefaultScenarioWhenNameIsBlank()
    {
        var resolver = new ScenarioResolver();

        var scenario = resolver.Resolve(null);

        Assert.Equal("Village Food Crisis", scenario.Name);
    }

    [Fact]
    public void ResolveReturnsCloneFromCatalog()
    {
        var catalogScenario = CreateValidScenario("Catalog Scenario");
        var catalog = new StubScenarioCatalog(catalogScenario);
        var resolver = new ScenarioResolver(catalog, new ThrowingScenarioLoader(), new ScenarioValidator());

        var resolved = resolver.Resolve("catalog-scenario");

        Assert.Equal("Catalog Scenario", resolved.Name);
        Assert.NotSame(catalogScenario, resolved);
        Assert.NotSame(catalogScenario.InitialResources, resolved.InitialResources);
    }

    [Fact]
    public void ResolveLoadsScenarioByExistingPathWhenCatalogMisses()
    {
        var loadedScenario = CreateValidScenario("Loaded Scenario");
        var loader = new StubScenarioLoader(loadedScenario);
        var path = Path.GetTempFileName();
        var resolver = new ScenarioResolver(new StubScenarioCatalog(null), loader, new ScenarioValidator());

        try
        {
            var resolved = resolver.Resolve(path);

            Assert.Equal("Loaded Scenario", resolved.Name);
            Assert.Equal(path, loader.LoadedPath);
            Assert.NotSame(loadedScenario, resolved);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ResolveDoesNotLoadScenarioByPathWhenFilePathsAreDisabled()
    {
        var loader = new ThrowingScenarioLoader();
        var path = Path.GetTempFileName();
        var resolver = new ScenarioResolver(
            new StubScenarioCatalog(null),
            loader,
            new ScenarioValidator(),
            allowFilePaths: false);

        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve(path));

            Assert.Contains($"Scenario '{path}' was not found.", exception.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ResolveThrowsWhenScenarioCannotBeFound()
    {
        var resolver = new ScenarioResolver(new StubScenarioCatalog(null), new ThrowingScenarioLoader(), new ScenarioValidator());

        var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("missing"));

        Assert.Contains("Scenario 'missing' was not found.", exception.Message);
    }

    [Fact]
    public void ResolveThrowsWhenScenarioIsInvalid()
    {
        var scenario = CreateValidScenario("");
        var resolver = new ScenarioResolver(new StubScenarioCatalog(scenario), new ThrowingScenarioLoader(), new ScenarioValidator());

        var exception = Assert.Throws<InvalidOperationException>(() => resolver.Resolve("invalid"));

        Assert.Contains("Scenario '' is invalid", exception.Message);
    }

    private static ScenarioDefinition CreateValidScenario(string name)
    {
        return new ScenarioDefinition
        {
            Name = name,
            Seed = 1,
            Ticks = 10,
            InitialPopulation = 1,
            InitialResources = new ResourcePool { Food = 1 }
        };
    }

    private sealed class StubScenarioCatalog(ScenarioDefinition? scenario) : IScenarioCatalog
    {
        public IReadOnlyList<ScenarioDefinition> All => scenario is null ? [] : [scenario];

        public ScenarioDefinition? FindByName(string name)
        {
            return scenario;
        }
    }

    private sealed class StubScenarioLoader(ScenarioDefinition scenario) : IScenarioLoader
    {
        public string? LoadedPath { get; private set; }

        public ScenarioDefinition Load(string path)
        {
            LoadedPath = path;
            return scenario;
        }
    }

    private sealed class ThrowingScenarioLoader : IScenarioLoader
    {
        public ScenarioDefinition Load(string path)
        {
            throw new InvalidOperationException("Loader should not have been called.");
        }
    }
}
