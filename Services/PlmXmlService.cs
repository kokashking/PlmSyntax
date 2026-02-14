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
    private Dictionary<string, string> _globalIdMap = new();

    private static void Log(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        Debug.WriteLine(line);
        try { File.AppendAllText(LogPath, line + Environment.NewLine); } catch { }
    }

    public async Task<PlmXml?> ParseAsync(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        var sw = Stopwatch.StartNew();
        Log($"ParseAsync START: {filePath}");
        _globalIdMap.Clear();

        return await Task.Run(() =>
        {
            try
            {
                var plmXml = new PlmXml();
                var settings = new XmlReaderSettings { IgnoreWhitespace = true };
                var elementCounts = new Dictionary<string, int>();
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                using var reader = XmlReader.Create(fileStream, settings);

                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element) continue;
                    string localName = reader.LocalName;
                    elementCounts[localName] = elementCounts.ContainsKey(localName) ? elementCounts[localName] + 1 : 1;
                    string? id = reader.GetAttribute("id");
                    if (!string.IsNullOrEmpty(id)) _globalIdMap[id] = localName;

                    if (localName == "InstanceGraph") {
                        plmXml.ProductDef.InstanceGraph.RootInstanceRef = reader.GetAttribute("rootInstanceRef") ?? "";
                    } else if (localName == "ProductInstance") {
                        var instance = new ProductInstance { Id = id ?? "", Name = reader.GetAttribute("name") ?? "", PartRef = reader.GetAttribute("partRef") ?? "", SequenceNumber = reader.GetAttribute("sequenceNumber") ?? "" };
                        if (!reader.IsEmptyElement) {
                            using var subReader = reader.ReadSubtree();
                            while (subReader.Read()) if (subReader.NodeType == XmlNodeType.Element && subReader.LocalName == "Transform")
                                instance.Transform = new Transform { Id = subReader.GetAttribute("id") ?? "", Value = subReader.ReadElementContentAsString() };
                        }
                        plmXml.ProductDef.InstanceGraph.ProductInstances.Add(instance);
                    } else if (localName == "ProductRevisionView") {
                        var view = new ProductRevisionView { Id = id ?? "", Name = reader.GetAttribute("name") ?? "", Type = reader.GetAttribute("type") ?? "", InstanceRefs = reader.GetAttribute("instanceRefs") ?? "", RevisionRef = reader.GetAttribute("revisionRef") ?? "" };
                        if (!reader.IsEmptyElement) {
                            using var subReader = reader.ReadSubtree();
                            var userValues = new List<UserValue>();
                            while (subReader.Read()) {
                                if (subReader.NodeType == XmlNodeType.Element) {
                                    if (subReader.LocalName == "Representation") view.Representation = new Representation { Id = subReader.GetAttribute("id") ?? "", Format = subReader.GetAttribute("format") ?? "", Location = subReader.GetAttribute("location") ?? "" };
                                    else if (subReader.LocalName == "UserValue") userValues.Add(new UserValue { Title = subReader.GetAttribute("title") ?? "", Value = subReader.GetAttribute("value") ?? "" });
                                }
                            }
                            view.UserData = userValues.ToArray();
                        }
                        plmXml.ProductDef.InstanceGraph.ProductRevisionViews.Add(view);
                    } else if (localName == "ProductRevision") {
                        plmXml.ProductRevisions.Add(new ProductRevision { Id = id ?? "", Name = reader.GetAttribute("name") ?? "", InstanceRefs = reader.GetAttribute("instanceRefs") ?? "" });
                    } else if (localName == "Product") {
                        plmXml.Products.Add(new Product { Id = id ?? "", Name = reader.GetAttribute("name") ?? "" });
                    }
                }
                CurrentPlmXml = plmXml;
                Log($"ParseAsync DONE. Elements: {string.Join(", ", elementCounts.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}");
                return plmXml;
            } catch (Exception ex) { Log($"ParseAsync EXCEPTION: {ex}"); return null; }
        });
    }

    public async Task<List<LeafNodeInfo>> TraverseTreeAsync(PlmXml plmXml)
    {
        var sw = Stopwatch.StartNew();
        Log($"TraverseTreeAsync START");

        return await Task.Run(() =>
        {
            var leafNodes = new List<LeafNodeInfo>();
            var instL = plmXml.ProductDef.InstanceGraph.ProductInstances.ToDictionary(x => x.Id, x => x);
            var viewL = plmXml.ProductDef.InstanceGraph.ProductRevisionViews.GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.First());
            var revL = plmXml.ProductRevisions.ToDictionary(x => x.Id, x => x);
            var prodL = plmXml.Products.ToDictionary(x => x.Id, x => x);

            string rootRef = plmXml.ProductDef.InstanceGraph.RootInstanceRef.TrimStart('#');
            ProductInstance? rootInstance = null;

            if (!instL.TryGetValue(rootRef, out rootInstance)) {
                Log($"  ROOT '{rootRef}' not in instances. searching for candidates...");
                // Find all instances NOT referenced by any view
                var referencedIds = new HashSet<string>(plmXml.ProductDef.InstanceGraph.ProductRevisionViews.SelectMany(v => v.InstanceRefs.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => s.TrimStart('#'))));
                rootInstance = plmXml.ProductDef.InstanceGraph.ProductInstances.FirstOrDefault(i => !referencedIds.Contains(i.Id));
                if (rootInstance != null) Log($"  Heuristic: Using non-referenced instance as root: '{rootInstance.Id}'");
                else {
                    rootInstance = plmXml.ProductDef.InstanceGraph.ProductInstances.LastOrDefault();
                    if (rootInstance != null) Log($"  Fallback: Using LAST instance: '{rootInstance.Id}'");
                }
            }

            if (rootInstance != null) {
                var stack = new Stack<(ProductInstance Instance, Matrix4x4 WorldMatrix, List<string> PartNumbers)>();
                stack.Push((rootInstance, Matrix4x4.Identity, new List<string>()));
                var visited = new HashSet<string>();
                while (stack.Count > 0) {
                    var (curr, pMat, pPNs) = stack.Pop();
                    string targetId = curr.PartRef.TrimStart('#');
                    ProductRevisionView? view = ResolveView(targetId, viewL, revL, prodL);
                    if (view == null) continue;
                    Matrix4x4 world = (curr.Transform?.Value.ParseTransform() ?? Matrix4x4.Identity) * pMat;
                    if (view.Representation != null) {
                        leafNodes.Add(new LeafNodeInfo { Name = view.Name, FileName = view.Representation.Location, Format = view.Representation.Format, ProductRevisionViewId = view.Id, Transform = world.ToExplicitDecimalString(), PartNumbers = string.Join(", ", pPNs) });
                        continue;
                    }
                    if (string.IsNullOrEmpty(view.InstanceRefs) || !visited.Add(curr.Id)) continue;
                    string pn = view.UserData.FirstOrDefault(u => u.Title == "Mehrnummern")?.Value ?? string.Empty;
                    var cPNs = string.IsNullOrEmpty(pn) ? pPNs : new List<string>(pPNs) { pn };
                    var childIds = view.InstanceRefs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    for (int i = childIds.Length - 1; i >= 0; i--) {
                        var cid = childIds[i].TrimStart('#');
                        if (instL.TryGetValue(cid, out var ci)) stack.Push((ci, world, cPNs));
                    }
                }
            }

            if (leafNodes.Count == 0) {
                Log("  Tree walk found NO leaves. Falling back to FLAT COLLECTION...");
                foreach (var view in plmXml.ProductDef.InstanceGraph.ProductRevisionViews.Where(v => v.Representation != null)) {
                    leafNodes.Add(new LeafNodeInfo { Name = view.Name, FileName = view.Representation.Location, Format = view.Representation.Format, ProductRevisionViewId = view.Id, Transform = Matrix4x4.Identity.ToExplicitDecimalString(), PartNumbers = "FLAT_FALLBACK" });
                }
            }

            Log($"TraverseTreeAsync DONE. Leaves: {leafNodes.Count}");
            CurrentLeafNodes = leafNodes;
            CurrentPlmXml = null;
            return leafNodes;
        });
    }

    private ProductRevisionView? ResolveView(string id, Dictionary<string, ProductRevisionView> vL, Dictionary<string, ProductRevision> rL, Dictionary<string, Product> pL) {
        if (vL.TryGetValue(id, out var v)) return v;
        if (rL.TryGetValue(id, out var r)) {
            foreach(var iid in r.InstanceRefs.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (vL.TryGetValue(iid.TrimStart('#'), out v)) return v;
        }
        return vL.Values.FirstOrDefault(x => x.RevisionRef == id || x.RevisionRef == "#" + id);
    }
}
