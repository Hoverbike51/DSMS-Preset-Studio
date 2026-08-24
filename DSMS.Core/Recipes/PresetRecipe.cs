namespace DSMS.Core.Recipes;

public sealed class PresetRecipe
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "Costume";
    public List<string> RequiredFields { get; set; } = [];
    public List<string> RecommendedFields { get; set; } = [];
    public List<string> Warnings { get; set; } = [];

    public override string ToString() => Name;
}
