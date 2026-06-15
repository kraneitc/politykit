using Microsoft.AspNetCore.Mvc;
using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Models;

namespace PolityKit.Sim.Api.Controllers;

[ApiController]
[Route("api/models")]
public sealed class ModelsController(
    IModelCatalog modelCatalog,
    GovernancePresetCatalog governancePresetCatalog) : ControllerBase
{
    [HttpGet]
    public IActionResult GetModels()
    {
        return Ok(modelCatalog.All.Select(ToResponse));
    }

    private ModelResponse ToResponse(ISystemModel model)
    {
        if (model is not AllocationModelBase allocationModel)
        {
            return new ModelResponse
            {
                Name = model.Name,
                Version = model.Version
            };
        }

        var manifest = allocationModel.Manifest;
        return new ModelResponse
        {
            Name = model.Name,
            Version = model.Version,
            Kind = model is CompositeGovernanceModel ? "governance-preset" : "baseline",
            Description = manifest.Description,
            Assumptions = manifest.Assumptions.Select(assumption => new AssumptionResponse
            {
                Name = assumption.Name,
                Default = assumption.Default,
                Description = assumption.Description
            }).ToArray(),
            KnownFailureModes = manifest.KnownFailureModes,
            Preset = ToPresetResponse(model)
        };
    }

    private GovernancePresetResponse? ToPresetResponse(ISystemModel model)
    {
        if (model is not CompositeGovernanceModel compositeModel)
        {
            return null;
        }

        var preset = governancePresetCatalog.FindById(compositeModel.Profile.Id);
        if (preset is null)
        {
            return new GovernancePresetResponse
            {
                Id = compositeModel.Profile.Id,
                Name = compositeModel.Profile.Name,
                Description = compositeModel.Profile.Description
            };
        }

        return new GovernancePresetResponse
        {
            Id = preset.Id,
            Name = preset.Name,
            Description = preset.Description,
            Assumptions = preset.Assumptions,
            KnownFailureModes = preset.KnownFailureModes
        };
    }
}
