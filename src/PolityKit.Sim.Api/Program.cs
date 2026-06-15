using System.Text.Json.Serialization;
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

        builder.Services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddProblemDetails();
        builder.Services.AddOpenApi();
        builder.Services.Configure<RunStorageOptions>(builder.Configuration.GetSection("RunStorage"));

        builder.Services.AddSingleton<ISimulationEngine, SimulationEngine>();
        builder.Services.AddSingleton(_ => new GovernancePresetCatalog());
        builder.Services.AddSingleton<IModelCatalog>(sp => new ModelCatalog(sp.GetRequiredService<GovernancePresetCatalog>()));
        builder.Services.AddSingleton<IMetricCatalog>(_ => new MetricCatalog());
        builder.Services.AddSingleton<IScenarioCatalog, BuiltInScenarioCatalog>();
        builder.Services.AddSingleton<IScenarioValidator, ScenarioValidator>();
        builder.Services.AddSingleton<IScenarioLoader, JsonScenarioLoader>();
        builder.Services.AddSingleton(sp => new ScenarioResolver(
            sp.GetRequiredService<IScenarioCatalog>(),
            sp.GetRequiredService<IScenarioLoader>(),
            sp.GetRequiredService<IScenarioValidator>(),
            allowFilePaths: false));
        builder.Services.AddSingleton<IRunStore, FileRunStore>();
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
