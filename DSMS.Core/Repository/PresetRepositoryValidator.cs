using System.Text.Json;
using DSMS.Core.Serialization;
using DSMS.Core.Validation;

namespace DSMS.Core.Repository;

public sealed class PresetRepositoryValidator(PresetValidator? validator = null)
{
    private readonly PresetValidator _validator = validator ?? new PresetValidator();

    public RepositoryValidationResult Scan(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
            throw new DirectoryNotFoundException(rootDirectory);

        var result = new RepositoryValidationResult();
        foreach (var file in Directory.EnumerateFiles(rootDirectory, "*.json", SearchOption.AllDirectories)
                     .Where(x => Path.GetFileName(x).StartsWith("DSMS-", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var preset = PresetSerializer.Deserialize(File.ReadAllText(file));
                result.Files.Add(new(file, preset, _validator.Validate(preset, Path.GetFileName(file))));
            }
            catch (JsonException exception)
            {
                var report = new ValidationReport();
                report.Error("DSMS100", "JSON", $"Invalid JSON: {exception.Message}");
                result.Files.Add(new(file, null, report));
            }
            catch (Exception exception)
            {
                var report = new ValidationReport();
                report.Error("DSMS101", "File", $"Could not read the preset: {exception.Message}");
                result.Files.Add(new(file, null, report));
            }
        }

        AddDuplicateIdIssues(result);
        return result;
    }

    private static void AddDuplicateIdIssues(RepositoryValidationResult result)
    {
        var groups = result.Files
            .Where(x => !string.IsNullOrWhiteSpace(x.Preset?.UniqueID))
            .GroupBy(x => x.Preset!.UniqueID!, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1);

        foreach (var group in groups)
        {
            var names = string.Join(", ", group.Select(x => Path.GetFileName(x.FilePath)));
            foreach (var file in group)
                file.Report.Error("DSMS102", "UniqueID", $"Duplicate UniqueID '{group.Key}' in: {names}");
        }
    }
}
