using PolityKit.Sim.Core.Models;

namespace PolityKit.Sim.Models;

public sealed class ModelCatalog(IEnumerable<ISystemModel> models) : IModelCatalog
{
    private readonly IReadOnlyList<ISystemModel> models = models.ToArray();

    public ModelCatalog()
        : this(DefaultModelSet.Create())
    {
    }

    public IReadOnlyList<ISystemModel> All => models;

    public ISystemModel? FindByName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return models.FirstOrDefault(model =>
            string.Equals(model.Name, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(ToKebabCase(model.Name), name, StringComparison.OrdinalIgnoreCase));
    }

    private static string ToKebabCase(string value)
    {
        var characters = new List<char>();

        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (index > 0 && char.IsUpper(character))
            {
                characters.Add('-');
            }

            characters.Add(char.ToLowerInvariant(character));
        }

        return new string([.. characters]);
    }
}
