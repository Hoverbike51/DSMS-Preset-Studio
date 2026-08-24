namespace DSMS.Core.Validation;

public enum ValidationSeverity { Info, Warning, Error }

public sealed record ValidationIssue(
    ValidationSeverity Severity,
    string Code,
    string Field,
    string Message);

public sealed class ValidationReport
{
    public List<ValidationIssue> Issues { get; } = [];
    public int ErrorCount => Issues.Count(x => x.Severity == ValidationSeverity.Error);
    public int WarningCount => Issues.Count(x => x.Severity == ValidationSeverity.Warning);
    public string RiskLevel => ErrorCount > 0 ? "Red" : WarningCount > 0 ? "Orange" : "Green";
    public bool IsValid => ErrorCount == 0;

    public void Error(string code, string field, string message) =>
        Issues.Add(new(ValidationSeverity.Error, code, field, message));
    public void Warning(string code, string field, string message) =>
        Issues.Add(new(ValidationSeverity.Warning, code, field, message));
    public void Info(string code, string field, string message) =>
        Issues.Add(new(ValidationSeverity.Info, code, field, message));
}
