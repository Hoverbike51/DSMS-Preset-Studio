using DSMS.Core.Models;
using DSMS.Core.Validation;

namespace DSMS.Core.Repository;

public sealed record PresetFileResult(
    string FilePath,
    PresetDocument? Preset,
    ValidationReport Report);

public sealed class RepositoryValidationResult
{
    public List<PresetFileResult> Files { get; } = [];
    public int FileCount => Files.Count;
    public int ErrorCount => Files.Sum(x => x.Report.ErrorCount);
    public int WarningCount => Files.Sum(x => x.Report.WarningCount);
    public int ValidFileCount => Files.Count(x => x.Report.IsValid);
}
