using System.Text.Json;

namespace DSMS.Core.Recipes;

public sealed class RecipeCatalog
{
    public IReadOnlyList<PresetRecipe> Recipes { get; }

    public RecipeCatalog(IEnumerable<PresetRecipe> recipes) => Recipes = recipes.ToList();

    public static RecipeCatalog Load(string filePath)
    {
        var values = JsonSerializer.Deserialize<List<PresetRecipe>>(File.ReadAllText(filePath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        return new RecipeCatalog(values);
    }
}
