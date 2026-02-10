using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml.Serialization;
using PlmXmlSyntax.Models;
using PlmXmlSyntax.Helpers;

namespace PlmXmlSyntax.Services;

public class PlmXmlService : IPlmXmlService
{
    public async Task<PlmXml?> ParseAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        return await Task.Run(() =>
        {
            try
            {
                var serializer = new XmlSerializer(typeof(PlmXml));
                using var reader = new StreamReader(filePath);
                return (PlmXml?)serializer.Deserialize(reader);
            }
            catch
            {
                return null;
            }
        });
    }

    // This is the core logic based on GEMINI.md and _DevHelp
    public async Task TraverseTreeAsync(PlmXml plmXml)
    {
        await Task.Run(() =>
        {
            var instanceLookup = plmXml.ProductDef.InstanceGraph.ProductInstances
                .ToDictionary(x => x.Id, x => x);

            var viewLookup = plmXml.ProductDef.InstanceGraph.ProductRevisionViews
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.First());

            if (!instanceLookup.TryGetValue(plmXml.ProductDef.InstanceGraph.RootInstanceRef, out var rootInstance))
                return;

            Stack<(ProductInstance Instance, Matrix4x4 ParentMatrix)> stack = new();
            stack.Push((rootInstance, Matrix4x4.Identity));

            while (stack.Count > 0)
            {
                var (currentInstance, parentMatrix) = stack.Pop();

                string viewId = currentInstance.PartRef.StartsWith("#") 
                    ? currentInstance.PartRef.Substring(1) 
                    : currentInstance.PartRef;

                if (!viewLookup.TryGetValue(viewId, out var view))
                    continue;

                Matrix4x4 localTransform = currentInstance.Transform?.Value.ParseTransform() ?? Matrix4x4.Identity;
                Matrix4x4 worldMatrix = localTransform * parentMatrix;

                if (view.Representation != null)
                {
                    // Found a leaf node
                    // For now we just calculate the transform, in future we might emit events or collect data
                    string transformStr = worldMatrix.ToExplicitDecimalString();
                }

                if (!string.IsNullOrEmpty(view.InstanceRefs))
                {
                    var childIds = view.InstanceRefs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var childId in childIds)
                    {
                        if (instanceLookup.TryGetValue(childId, out var childInstance))
                        {
                            stack.Push((childInstance, worldMatrix));
                        }
                    }
                }
            }
        });
    }
}
