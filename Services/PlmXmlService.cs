using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Xml;
using System.Xml.Serialization;
using PlmXmlSyntax.Helpers;
using PlmXmlSyntax.Models;

namespace PlmXmlSyntax.Services;

public class PlmXmlService : IPlmXmlService
{
    private static readonly string LogPath = Path.Combine(
        Path.GetDirectoryName(AppContext.BaseDirectory) ?? ".", "plmxml_diag.log");

    public PlmXml? CurrentPlmXml { get; private set; }
    public List<LeafNodeInfo>? CurrentLeafNodes { get; private set; }

    private static void Log(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        Debug.WriteLine(line);
        try { File.AppendAllText(LogPath, line + Environment.NewLine); } catch { }
    }

    public async Task<PlmXml?> ParseAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var sw = Stopwatch.StartNew();
        Log($"ParseAsync START (Streaming): {filePath}");
        Log($"File size: {new FileInfo(filePath).Length / 1024.0 / 1024.0:F1} MB");
        Log($"Process: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");

        return await Task.Run(() =>
        {
            try
            {
                var plmXml = new PlmXml();
                var settings = new XmlReaderSettings { IgnoreWhitespace = true };
                
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                using var reader = XmlReader.Create(fileStream, settings);

                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element) continue;

                    string localName = reader.LocalName;
                    if (localName == "InstanceGraph")
                    {
                        plmXml.ProductDef.InstanceGraph.RootInstanceRef = reader.GetAttribute("rootInstanceRef") ?? "";
                    }
                    else if (localName == "ProductInstance")
                    {
                        var instance = new ProductInstance
                        {
                            Id = reader.GetAttribute("id") ?? "",
                            Name = reader.GetAttribute("name") ?? "",
                            PartRef = reader.GetAttribute("partRef") ?? "",
                            SequenceNumber = reader.GetAttribute("sequenceNumber") ?? ""
                        };
                        
                        if (!reader.IsEmptyElement)
                        {
                            using var subReader = reader.ReadSubtree();
                            while (subReader.Read())
                            {
                                if (subReader.NodeType == XmlNodeType.Element && subReader.LocalName == "Transform")
                                {
                                    instance.Transform = new Transform { 
                                        Id = subReader.GetAttribute("id") ?? "",
                                        Value = subReader.ReadElementContentAsString() 
                                    };
                                }
                            }
                        }
                        plmXml.ProductDef.InstanceGraph.ProductInstances.Add(instance);
                    }
                    else if (localName == "ProductRevisionView")
                    {
                        var view = new ProductRevisionView
                        {
                            Id = reader.GetAttribute("id") ?? "",
                            Name = reader.GetAttribute("name") ?? "",
                            Type = reader.GetAttribute("type") ?? "",
                            InstanceRefs = reader.GetAttribute("instanceRefs") ?? "",
                            RevisionRef = reader.GetAttribute("revisionRef") ?? ""
                        };

                        if (!reader.IsEmptyElement)
                        {
                            using var subReader = reader.ReadSubtree();
                            var userValues = new List<UserValue>();
                            while (subReader.Read())
                            {
                                if (subReader.NodeType == XmlNodeType.Element)
                                {
                                    if (subReader.LocalName == "Representation")
                                    {
                                        view.Representation = new Representation
                                        {
                                            Id = subReader.GetAttribute("id") ?? "",
                                            Format = subReader.GetAttribute("format") ?? "",
                                            Location = subReader.GetAttribute("location") ?? ""
                                        };
                                    }
                                    else if (subReader.LocalName == "UserValue")
                                    {
                                        userValues.Add(new UserValue {
                                            Title = subReader.GetAttribute("title") ?? "",
                                            Value = subReader.GetAttribute("value") ?? ""
                                        });
                                    }
                                }
                            }
                            view.UserData = userValues.ToArray();
                        }
                        plmXml.ProductDef.InstanceGraph.ProductRevisionViews.Add(view);
                    }
                }

                CurrentPlmXml = plmXml;
                Log($"ParseAsync DONE in {sw.ElapsedMilliseconds}ms");
                Log($"  Instances: {plmXml.ProductDef.InstanceGraph.ProductInstances.Count}");
                Log($"  Views: {plmXml.ProductDef.InstanceGraph.ProductRevisionViews.Count}");
                Log($"  Memory: {GC.GetTotalMemory(false) / 1024.0 / 1024.0:F1} MB");

                return plmXml;
            }
            catch (Exception ex)
            {
                Log($"ParseAsync EXCEPTION: {ex}");
                return null;
            }
        });
    }

    public async Task<List<LeafNodeInfo>> TraverseTreeAsync(PlmXml plmXml)
    {
        var sw = Stopwatch.StartNew();
        Log($"TraverseTreeAsync START");

        return await Task.Run(() =>
        {
            var leafNodes = new List<LeafNodeInfo>();

            var instanceLookup = plmXml.ProductDef.InstanceGraph.ProductInstances
                .ToDictionary(x => x.Id, x => x);

            var viewLookup = plmXml.ProductDef.InstanceGraph.ProductRevisionViews
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.First());

            Log($"  Lookups built in {sw.ElapsedMilliseconds}ms (instances={instanceLookup.Count}, views={viewLookup.Count})");

            string rootRef = plmXml.ProductDef.InstanceGraph.RootInstanceRef.TrimStart('#');
            if (!instanceLookup.TryGetValue(rootRef, out var rootInstance))
            {
                Log($"  ROOT NOT FOUND: '{rootRef}' — first 5 IDs: {string.Join(", ", instanceLookup.Keys.Take(5))}");
                return leafNodes;
            }

            Log($"  Root found: '{rootRef}' -> PartRef='{rootInstance.PartRef}'");

            var stack = new Stack<(ProductInstance Instance, Matrix4x4 WorldMatrix, List<string> PartNumbers)>();
            stack.Push((rootInstance, Matrix4x4.Identity, new List<string>()));
            
            // We only need to track visited ASSEMBLY instances to prevent infinite loops.
            // Shared leaf instances should be allowed to be visited multiple times if they appear in different branches.
            var visitedAssemblies = new HashSet<string>();
            int iterations = 0;

            while (stack.Count > 0)
            {
                var (currentInstance, parentWorldMatrix, parentPartNumbers) = stack.Pop();
                iterations++;

                if (iterations % 1000 == 0)
                    Log($"  ... iter {iterations}, stack={stack.Count}, leaves={leafNodes.Count}, mem={GC.GetTotalMemory(false) / 1024.0 / 1024.0:F1}MB");

                string viewId = currentInstance.PartRef.StartsWith('#')
                    ? currentInstance.PartRef.Substring(1)
                    : currentInstance.PartRef;

                if (!viewLookup.TryGetValue(viewId, out var view))
                {
                    continue;
                }

                Matrix4x4 localTransform = currentInstance.Transform?.Value.ParseTransform() ?? Matrix4x4.Identity;
                Matrix4x4 worldMatrix = localTransform * parentWorldMatrix;

                // Leaf node (has Representation)
                if (view.Representation != null)
                {
                    leafNodes.Add(new LeafNodeInfo
                    {
                        Name = view.Name,
                        FileName = view.Representation.Location,
                        Format = view.Representation.Format,
                        ProductRevisionViewId = view.Id,
                        Transform = worldMatrix.ToExplicitDecimalString(),
                        PartNumbers = string.Join(", ", parentPartNumbers)
                    });
                    continue;
                }

                // Assembly node (has children)
                if (string.IsNullOrEmpty(view.InstanceRefs))
                    continue;

                // Prevent infinite recursion in assemblies
                if (!visitedAssemblies.Add(currentInstance.Id))
                {
                    continue;
                }

                string partNumber = view.UserData
                    .FirstOrDefault(u => u.Title == "Mehrnummern")?.Value ?? string.Empty;
                var currentPartNumbers = !string.IsNullOrEmpty(partNumber)
                    ? new List<string>(parentPartNumbers) { partNumber }
                    : parentPartNumbers;

                var childIds = view.InstanceRefs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                // Push in reverse to maintain order if desired, but for DFS it doesn't strictly matter
                for (int i = childIds.Length - 1; i >= 0; i--)
                {
                    var childId = childIds[i].TrimStart('#');
                    if (instanceLookup.TryGetValue(childId, out var childInstance))
                    {
                        stack.Push((childInstance, worldMatrix, currentPartNumbers));
                    }
                }
            }

            Log($"TraverseTreeAsync DONE in {sw.ElapsedMilliseconds}ms");
            Log($"  Total iterations: {iterations}");
            Log($"  Leaf nodes found: {leafNodes.Count}");
            Log($"  Memory: {GC.GetTotalMemory(false) / 1024.0 / 1024.0:F1} MB");

            CurrentLeafNodes = leafNodes;
            CurrentPlmXml = null; // Clear the heavy raw data
            GC.Collect(); // Suggest GC collect the large graph
            return leafNodes;
        });
    }
}
