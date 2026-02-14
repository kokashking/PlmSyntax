using System.Collections.Generic;
using System.Threading.Tasks;
using PlmXmlSyntax.Models;

namespace PlmXmlSyntax.Services;

public interface IPlmXmlAuditService
{
    /// <summary>
    /// Toggle to completely bypass audit logic for maximum performance.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Performs a deep health check on a parsed PLMXML object.
    /// </summary>
    Task<List<AuditIssue>> AuditAsync(PlmXml plmXml, string sourcePath);

    /// <summary>
    /// Summarizes issues found across multiple files (e.g. ID collisions).
    /// </summary>
    Task<List<AuditIssue>> AnalyzeCrossFileCollisionsAsync(IEnumerable<(PlmXml Data, string Path)> datasets);
}
