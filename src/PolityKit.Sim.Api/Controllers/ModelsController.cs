using Microsoft.AspNetCore.Mvc;
using PolityKit.Sim.Api.Contracts;
using PolityKit.Sim.Core.Models;
using PolityKit.Sim.Models;

namespace PolityKit.Sim.Api.Controllers;

[ApiController]
[Route("api/models")]
public sealed class ModelsController(IModelCatalog modelCatalog) : ControllerBase
{
    [HttpGet]
    public IActionResult GetModels()
    {
        return Ok(modelCatalog.All.Select(ToResponse));
    }

    private static ModelResponse ToResponse(ISystemModel model)
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
            Description = manifest.Description,
            Assumptions = manifest.Assumptions.Select(assumption => new AssumptionResponse
            {
                Name = assumption.Name,
                Default = assumption.Default,
                Description = assumption.Description
            }).ToArray(),
            KnownFailureModes = manifest.KnownFailureModes
        };
    }
}
