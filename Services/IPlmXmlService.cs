using PlmXmlSyntax.Models;

namespace PlmXmlSyntax.Services;

public interface IPlmXmlService
{
    PlmXml? CurrentPlmXml { get; }
    List<LeafNodeInfo>? CurrentLeafNodes { get; }

    Task<PlmXml?> ParseAsync(string filePath);
    Task<List<LeafNodeInfo>> TraverseTreeAsync(PlmXml plmXml);
}
