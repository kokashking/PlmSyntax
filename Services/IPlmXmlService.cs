using System.IO;
using System.Threading.Tasks;
using PlmXmlSyntax.Models;

namespace PlmXmlSyntax.Services;

public interface IPlmXmlService
{
    Task<PlmXml?> ParseAsync(string filePath);
    Task TraverseTreeAsync(PlmXml plmXml);
}
