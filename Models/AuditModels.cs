namespace PlmXmlSyntax.Models;

public enum AuditSeverity
{
    Info,    // Resolved (e.g. ID Collision fixed)
    Warning, // Potential issue (e.g. Empty assembly)
    Error    // Hard break (e.g. Missing geometry reference)
}

public enum AuditCategory
{
    Collision,
    MissingReference,
    Schema,
    Performance
}

public class AuditIssue
{
    public string NodeId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public AuditSeverity Severity { get; init; }
    public AuditCategory Category { get; init; }
    public string SourceFile { get; init; } = string.Empty;
}
