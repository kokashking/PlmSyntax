using System.Collections.Generic;
using System.Threading.Tasks;
using PlmXmlSyntax.Models;

namespace PlmXmlSyntax.Services;

public interface IPlmXmlMergeService
{
    /// <summary>
    /// Merges multiple PLMXML files into a single output file, resolving all ID collisions.
    /// </summary>
    Task<bool> MergeFilesAsync(IEnumerable<string> sourceFilePaths, string outputFilePath, string masterName);
}
