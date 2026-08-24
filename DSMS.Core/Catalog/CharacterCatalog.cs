using System.Text.Json;

namespace DSMS.Core.Catalog;

public sealed class CharacterCatalog
{
    private readonly List<CharacterDefinition> _characters;
    public IReadOnlyList<CharacterDefinition> Characters => _characters;

    public CharacterCatalog(IEnumerable<CharacterDefinition> characters) =>
        _characters = characters.OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();

    public CharacterDefinition? Find(string? idOrAlias)
    {
        if (string.IsNullOrWhiteSpace(idOrAlias)) return null;
        return _characters.FirstOrDefault(x =>
            x.InternalId.Equals(idOrAlias, StringComparison.OrdinalIgnoreCase) ||
            x.DisplayName.Equals(idOrAlias, StringComparison.OrdinalIgnoreCase) ||
            x.Aliases.Any(a => a.Equals(idOrAlias, StringComparison.OrdinalIgnoreCase)));
    }

    public static CharacterCatalog Load(string filePath)
    {
        var values = JsonSerializer.Deserialize<List<CharacterDefinition>>(File.ReadAllText(filePath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        return new CharacterCatalog(values);
    }
}
