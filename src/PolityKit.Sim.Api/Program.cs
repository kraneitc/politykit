
using PolityKit.Sim.Api.Services;
using PolityKit.Sim.Engine;
using PolityKit.Sim.Metrics;
using PolityKit.Sim.Models;
using PolityKit.Sim.Scenarios;

namespace PolityKit.Sim.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();

        builder.Services.AddSingleton<ISimulationEngine, SimulationEngine>();
        builder.Services.AddSingleton<IModelCatalog>(_ => new ModelCatalog());
        builder.Services.AddSingleton<IMetricCatalog>(_ => new MetricCatalog());
        builder.Services.AddSingleton<IScenarioCatalog, BuiltInScenarioCatalog>();
        builder.Services.AddSingleton<IScenarioValidator, ScenarioValidator>();
        builder.Services.AddSingleton<IScenarioLoader, JsonScenarioLoader>();
        builder.Services.AddSingleton<ScenarioResolver>();
        builder.Services.AddSingleton<IRunStore, InMemoryRunStore>();
        builder.Services.AddSingleton<SimulationRunService>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
