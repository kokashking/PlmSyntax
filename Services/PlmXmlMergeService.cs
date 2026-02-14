using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlmXmlSyntax.Services;

public class PlmXmlMergeService : IPlmXmlMergeService
{
    private static void Log(string msg) => Debug.WriteLine($"[Merge] {msg}");

    public async Task<bool> MergeFilesAsync(IEnumerable<string> sourceFilePaths, string outputFilePath, string masterName)
    {
        return await Task.Run(() =>
        {
            try
            {
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
                    var prefix = $"F{fileIndex}_";
                    var sourceRootId = ProcessFileContent(sourcePath, writer, prefix);
                    if (!string.IsNullOrEmpty(sourceRootId))
                    {
                        rootInstanceIds.Add(sourceRootId);
                    }
                    fileIndex++;
                }

                // 4. Create the Master Assembly Structure
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

    private string ProcessFileContent(string sourcePath, XmlWriter writer, string prefix)
    {
        string rootInstanceId = "";
        using var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
        using var reader = XmlReader.Create(stream);

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                var name = reader.LocalName;

                // Skip top-level structural elements that we manage ourselves
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
                
                // Process all attributes, renaming IDs and Refs
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

        // 1. Create the Master Root View (Assembly)
        writer.WriteStartElement("ProductRevisionView");
        writer.WriteAttributeString("id", "MasterRootView");
        writer.WriteAttributeString("name", masterName);
        writer.WriteAttributeString("type", "assembly");
        
        // Link all sub-roots as instances of this master view
        var instanceRefs = string.Join(" ", subRootIds);
        writer.WriteAttributeString("instanceRefs", instanceRefs);
        writer.WriteEndElement();

        // 2. Create the Master Root Instance
        writer.WriteStartElement("ProductInstance");
        writer.WriteAttributeString("id", "MasterRootInst");
        writer.WriteAttributeString("name", masterName);
        writer.WriteAttributeString("partRef", "#MasterRootView");
        writer.WriteEndElement();

        writer.WriteEndElement(); // InstanceGraph
        writer.WriteEndElement(); // ProductDef
    }

    private bool IsIdAttribute(string name) => name == "id";

    private bool IsReferenceAttribute(string name) => 
        name == "partRef" || name == "instanceRefs" || name == "revisionRef" || 
        name == "memberRefs" || name == "locationRef" || name == "dataSetRef";

    private string RenameReference(string value, string prefix)
    {
        if (string.IsNullOrEmpty(value)) return value;

        // Handle space-separated lists (like instanceRefs="id1 id2")
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].StartsWith("#"))
                parts[i] = "#" + prefix + parts[i].Substring(1);
            else
                parts[i] = prefix + parts[i];
        }
        return string.Join(" ", parts);
    }
}
