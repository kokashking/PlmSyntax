using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlmXmlSyntax.Models;

namespace PlmXmlSyntax.Services;

public class PlmXmlAuditService : IPlmXmlAuditService
{
    public bool IsEnabled { get; set; } = true;

    public async Task<List<AuditIssue>> AuditAsync(PlmXml plmXml, string sourcePath)
    {
        if (!IsEnabled) return new List<AuditIssue>();

        return await Task.Run(() =>
        {
            var issues = new List<AuditIssue>();
            
            var allIds = new HashSet<string>();
            foreach (var inst in plmXml.ProductDef.InstanceGraph.ProductInstances) if(!string.IsNullOrEmpty(inst.Id)) allIds.Add(inst.Id.Trim());
            foreach (var view in plmXml.ProductDef.InstanceGraph.ProductRevisionViews) if(!string.IsNullOrEmpty(view.Id)) allIds.Add(view.Id.Trim());
            foreach (var rev in plmXml.ProductRevisions) if(!string.IsNullOrEmpty(rev.Id)) allIds.Add(rev.Id.Trim());
            foreach (var prod in plmXml.Products) if(!string.IsNullOrEmpty(prod.Id)) allIds.Add(prod.Id.Trim());

            foreach (var inst in plmXml.ProductDef.InstanceGraph.ProductInstances)
            {
                var target = inst.PartRef.TrimStart('#').Trim();
                if (!string.IsNullOrEmpty(target) && !allIds.Contains(target))
                {
                    issues.Add(new AuditIssue { NodeId = inst.Id, Category = AuditCategory.MissingReference, Severity = AuditSeverity.Error, Message = $"Instance references missing Part/View: '{target}'", SourceFile = sourcePath });
                }
            }

            foreach (var view in plmXml.ProductDef.InstanceGraph.ProductRevisionViews)
            {
                if (view.Type == "assembly" && string.IsNullOrEmpty(view.InstanceRefs) && view.Representation == null)
                {
                    issues.Add(new AuditIssue { NodeId = view.Id, Category = AuditCategory.Performance, Severity = AuditSeverity.Warning, Message = $"Empty assembly node (no children or geometry)", SourceFile = sourcePath });
                }
            }

            return issues;
        });
    }

    public async Task<List<AuditIssue>> AnalyzeCrossFileCollisionsAsync(IEnumerable<(PlmXml Data, string Path)> datasets)
    {
        if (!IsEnabled) return new List<AuditIssue>();

        return await Task.Run(() =>
        {
            var issues = new List<AuditIssue>();
            // Key: ID, Value: (Type of Element, Source Path)
            var globalIdMap = new Dictionary<string, (string Type, string Path)>();

            foreach (var (data, path) in datasets)
            {
                // Local map to track types in current file
                var currentFileIds = new Dictionary<string, string>();
                
                foreach (var x in data.ProductDef.InstanceGraph.ProductInstances) if(!string.IsNullOrEmpty(x.Id)) currentFileIds[x.Id.Trim()] = "ProductInstance";
                foreach (var x in data.ProductDef.InstanceGraph.ProductRevisionViews) if(!string.IsNullOrEmpty(x.Id)) currentFileIds[x.Id.Trim()] = "ProductRevisionView";
                foreach (var x in data.ProductRevisions) if(!string.IsNullOrEmpty(x.Id)) currentFileIds[x.Id.Trim()] = "ProductRevision";
                foreach (var x in data.Products) if(!string.IsNullOrEmpty(x.Id)) currentFileIds[x.Id.Trim()] = "Product";

                foreach (var kvp in currentFileIds)
                {
                    var id = kvp.Key;
                    var type = kvp.Value;

                    if (globalIdMap.TryGetValue(id, out var firstSeen))
                    {
                        issues.Add(new AuditIssue
                        {
                            NodeId = id,
                            Category = AuditCategory.Collision,
                            Severity = AuditSeverity.Warning,
                            Message = $"ID '{id}' ({type}) collides with a {firstSeen.Type} in '{System.IO.Path.GetFileName(firstSeen.Path)}'.",
                            SourceFile = path
                        });
                    }
                    else
                    {
                        globalIdMap[id] = (type, path);
                    }
                }
            }

            return issues;
        });
    }
}
