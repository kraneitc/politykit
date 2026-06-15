using PolityKit.Sim.Core.Models;
using System.Text;

namespace PolityKit.Sim.Models;

public sealed class ModelCatalog(IEnumerable<ISystemModel> models) : IModelCatalog
{
    private readonly IReadOnlyList<ISystemModel> _models = models.ToArray();

    public ModelCatalog()
        : this(new GovernancePresetCatalog())
    {
    }

    public ModelCatalog(GovernancePresetCatalog governancePresetCatalog)
        : this(DefaultModelSet.Create(governancePresetCatalog))
    {
    }

    public IReadOnlyList<ISystemModel> All => _models;

    public ISystemModel? FindByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalizedName = name.Trim();
        if (TryGetPresetId(normalizedName, out var presetId))
        {
            return FindByPresetId(presetId);
        }

        return _models.FirstOrDefault(model =>
            string.Equals(model.Name, normalizedName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(ToKebabCase(model.Name), normalizedName, StringComparison.OrdinalIgnoreCase))
            ?? FindByPresetId(normalizedName)
            ?? FindByPresetName(normalizedName);
    }

    private static string ToKebabCase(string value)
    {
        var builder = new StringBuilder();
        var previousWasSeparator = false;

        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                if (char.IsUpper(character) && builder.Length > 0 && !previousWasSeparator)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                continue;
            }

            if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return previousWasSeparator
            ? builder.ToString(0, builder.Length - 1)
            : builder.ToString();
    }

    private static bool TryGetPresetId(string name, out string presetId)
    {
        const string prefix = "preset:";
        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            presetId = name[prefix.Length..].Trim();
            return !string.IsNullOrWhiteSpace(presetId);
        }

        presetId = "";
        return false;
    }

    private ISystemModel? FindByPresetId(string presetId)
    {
        return _models
            .OfType<CompositeGovernanceModel>()
            .FirstOrDefault(model => string.Equals(model.Profile.Id, presetId, StringComparison.OrdinalIgnoreCase));
    }

    private ISystemModel? FindByPresetName(string presetName)
    {
        return _models
            .OfType<CompositeGovernanceModel>()
            .FirstOrDefault(model =>
                string.Equals(model.Profile.Name, presetName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(ToKebabCase(model.Profile.Name), presetName, StringComparison.OrdinalIgnoreCase));
    }
}
