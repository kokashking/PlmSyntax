using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using PlmXmlSyntax.Models;

namespace PlmXmlSyntax.Services;

public class PlmXmlMergeService : IPlmXmlMergeService
{
    private readonly IProfileService _profileService;
    private static void Log(string msg) => Debug.WriteLine($"[Merge] {msg}");

    // Map to track unified IDs (e.g., "Bolt_M10" -> "Unified_id5")
    private readonly Dictionary<string, string> _unifiedIdMap = new();
    private int _unifiedCounter = 0;

    public PlmXmlMergeService(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task<bool> MergeFilesAsync(IEnumerable<string> sourceFilePaths, string outputFilePath, string masterName)
    {
        return await Task.Run(() =>
        {
            try
            {
                var profile = _profileService.GetActiveProfile();
                _unifiedIdMap.Clear();
                _unifiedCounter = 0;

                var settings = new XmlWriterSettings { Indent = true, Async = true };
                using var outputStream = new FileStream(outputFilePath, FileMode.Create);
                using var writer = XmlWriter.Create(outputStream, settings);

                writer.WriteStartDocument();
                writer.WriteStartElement("PLMXML", "http://www.plmxml.org/Schemas/PLMXMLSchema");
                writer.WriteAttributeString("author", "PlmXmlSyntax Merged");
                writer.WriteAttributeString("date", DateTime.Now.ToString("yyyy-MM-dd"));
                writer.WriteAttributeString("time", DateTime.Now.ToString("HH:mm:ss"));

                var fileIndex = 1;
                var rootInstanceIds = new List<string>();

                foreach (var sourcePath in sourceFilePaths)
                {
                    var prefix = string.Format(profile.MergePrefixPattern, fileIndex);
                    
                    // If we are unifying, we might need a pre-pass or specific logic in ProcessFileContent
                    var sourceRootId = ProcessFileContent(sourcePath, writer, prefix, profile);
                    if (!string.IsNullOrEmpty(sourceRootId))
                    {
                        rootInstanceIds.Add(sourceRootId);
                    }
                    fileIndex++;
                }

                WriteMasterStructure(writer, rootInstanceIds, masterName);

                writer.WriteEndElement(); // PLMXML
                writer.WriteEndDocument();
                writer.Flush();

                return true;
            }
            catch (Exception ex)
            {
                Log($"Merge Failed: {ex.Message}");
                return false;
            }
        });
    }

    private string ProcessFileContent(string sourcePath, XmlWriter writer, string prefix, ParsingProfile profile)
    {
        string rootInstanceId = "";
        using var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
        using var reader = XmlReader.Create(stream);

        // Note: Full Unification by Metadata usually requires a two-pass approach or 
        // a more complex mapping. For "Minimal Code", we implement the Prefix logic
        // and allow expansion for Unification.

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                var name = reader.LocalName;

                if (name == "PLMXML" || name == "ProductDef" || name == "InstanceGraph")
                {
                    if (name == "InstanceGraph")
                    {
                        var rawRoot = reader.GetAttribute("rootInstanceRef") ?? "";
                        rootInstanceId = prefix + rawRoot.TrimStart('#');
                    }
                    continue;
                }

                writer.WriteStartElement(reader.Prefix, name, reader.NamespaceURI);
                
                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        var attrName = reader.LocalName;
                        var attrValue = reader.Value;

                        if (IsIdAttribute(attrName))
                        {
                            writer.WriteAttributeString(attrName, prefix + attrValue);
                        }
                        else if (IsReferenceAttribute(attrName))
                        {
                            writer.WriteAttributeString(attrName, RenameReference(attrValue, prefix));
                        }
                        else
                        {
                            // Logic for Guard Tool (IP Protection)
                            if (profile.ScrubUserNames && (attrName == "author" || attrName == "owner"))
                                writer.WriteAttributeString(attrName, "ANONYMOUS");
                            else
                                writer.WriteAttributeString(attrName, attrValue);
                        }
                    }
                    reader.MoveToElement();
                }

                if (reader.IsEmptyElement) writer.WriteEndElement();
            }
            else if (reader.NodeType == XmlNodeType.EndElement)
            {
                var name = reader.LocalName;
                if (name != "PLMXML" && name != "ProductDef" && name != "InstanceGraph")
                {
                    writer.WriteEndElement();
                }
            }
            else if (reader.NodeType == XmlNodeType.Text)
            {
                writer.WriteString(reader.Value);
            }
        }

        return rootInstanceId;
    }

    private void WriteMasterStructure(XmlWriter writer, List<string> subRootIds, string masterName)
    {
        writer.WriteStartElement("ProductDef");
        writer.WriteStartElement("InstanceGraph");
        writer.WriteAttributeString("id", "MasterGraph");
        writer.WriteAttributeString("rootInstanceRef", "#MasterRootInst");

        writer.WriteStartElement("ProductRevisionView");
        writer.WriteAttributeString("id", "MasterRootView");
        writer.WriteAttributeString("name", masterName);
        writer.WriteAttributeString("type", "assembly");
        writer.WriteAttributeString("instanceRefs", string.Join(" ", subRootIds));
        writer.WriteEndElement();

        writer.WriteStartElement("ProductInstance");
        writer.WriteAttributeString("id", "MasterRootInst");
        writer.WriteAttributeString("name", masterName);
        writer.WriteAttributeString("partRef", "#MasterRootView");
        writer.WriteEndElement();

        writer.WriteEndElement(); 
        writer.WriteEndElement();
    }

    private bool IsIdAttribute(string name) => name == "id";
    private bool IsReferenceAttribute(string name) => 
        name == "partRef" || name == "instanceRefs" || name == "revisionRef" || 
        name == "memberRefs" || name == "locationRef" || name == "dataSetRef";

    private string RenameReference(string value, string prefix)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].StartsWith("#")) parts[i] = "#" + prefix + parts[i].Substring(1);
            else parts[i] = prefix + parts[i];
        }
        return string.Join(" ", parts);
    }
}
