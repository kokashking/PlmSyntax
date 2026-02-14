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
            
            // Map all IDs in the file for quick lookup (with trimming)
            var allIds = new HashSet<string>();
            foreach (var inst in plmXml.ProductDef.InstanceGraph.ProductInstances) if(!string.IsNullOrEmpty(inst.Id)) allIds.Add(inst.Id.Trim());
            foreach (var view in plmXml.ProductDef.InstanceGraph.ProductRevisionViews) if(!string.IsNullOrEmpty(view.Id)) allIds.Add(view.Id.Trim());
            foreach (var rev in plmXml.ProductRevisions) if(!string.IsNullOrEmpty(rev.Id)) allIds.Add(rev.Id.Trim());
            foreach (var prod in plmXml.Products) if(!string.IsNullOrEmpty(prod.Id)) allIds.Add(prod.Id.Trim());

            // Check for Broken References
            foreach (var inst in plmXml.ProductDef.InstanceGraph.ProductInstances)
            {
                var target = inst.PartRef.TrimStart('#').Trim();
                if (!string.IsNullOrEmpty(target) && !allIds.Contains(target))
                {
                    issues.Add(new AuditIssue
                    {
                        NodeId = inst.Id,
                        Category = AuditCategory.MissingReference,
                        Severity = AuditSeverity.Error,
                        Message = $"Instance references missing Part/View: '{target}'",
                        SourceFile = sourcePath
                    });
                }
            }

            // Check for Empty Assemblies
            foreach (var view in plmXml.ProductDef.InstanceGraph.ProductRevisionViews)
            {
                if (view.Type == "assembly" && string.IsNullOrEmpty(view.InstanceRefs) && view.Representation == null)
                {
                    issues.Add(new AuditIssue
                    {
                        NodeId = view.Id,
                        Category = AuditCategory.Performance,
                        Severity = AuditSeverity.Warning,
                        Message = $"Empty assembly node (no children or geometry)",
                        SourceFile = sourcePath
                    });
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
            var globalIdMap = new Dictionary<string, string>(); // Id -> First Source Path

            foreach (var (data, path) in datasets)
            {
                var currentIds = new HashSet<string>();
                
                // Collect ALL IDs from this file
                foreach (var inst in data.ProductDef.InstanceGraph.ProductInstances) if(!string.IsNullOrEmpty(inst.Id)) currentIds.Add(inst.Id.Trim());
                foreach (var view in data.ProductDef.InstanceGraph.ProductRevisionViews) if(!string.IsNullOrEmpty(view.Id)) currentIds.Add(view.Id.Trim());
                foreach (var rev in data.ProductRevisions) if(!string.IsNullOrEmpty(rev.Id)) currentIds.Add(rev.Id.Trim());
                foreach (var prod in data.Products) if(!string.IsNullOrEmpty(prod.Id)) currentIds.Add(prod.Id.Trim());

                foreach (var id in currentIds)
                {
                    if (globalIdMap.TryGetValue(id, out var firstSeenPath))
                    {
                        issues.Add(new AuditIssue
                        {
                            NodeId = id,
                            Category = AuditCategory.Collision,
                            Severity = AuditSeverity.Warning,
                            Message = $"ID Collision: '{id}' also exists in '{System.IO.Path.GetFileName(firstSeenPath)}'.",
                            SourceFile = path
                        });
                    }
                    else
                    {
                        globalIdMap[id] = path;
                    }
                }
            }

            return issues;
        });
    }
}
