namespace DSMS.Core.Catalog;

public sealed class CharacterDefinition
{
    public string InternalId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public List<string> Aliases { get; set; } = [];
    public bool Playable { get; set; }
    public string Notes { get; set; } = "";

    public override string ToString() => DisplayName;
}
