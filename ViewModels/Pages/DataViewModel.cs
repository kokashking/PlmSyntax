using System.Collections.ObjectModel;
using PlmXmlSyntax.Models;
using PlmXmlSyntax.Services;
using Wpf.Ui.Abstractions.Controls;

namespace PlmXmlSyntax.ViewModels.Pages
{
    public partial class DataViewModel : ObservableObject, INavigationAware
    {
        private readonly IPlmXmlService _plmXmlService;

        [ObservableProperty]
        private ObservableCollection<TreeNodeViewModel> _treeNodes = new();

        [ObservableProperty]
        private string _status = "No data loaded. Import a PLMXML file first.";

        public DataViewModel(IPlmXmlService plmXmlService)
        {
            _plmXmlService = plmXmlService;
        }

        public Task OnNavigatedToAsync()
        {
            BuildTree();
            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void BuildTree()
        {
            TreeNodes.Clear();

            var plmXml = _plmXmlService.CurrentPlmXml;
            if (plmXml == null)
            {
                Status = "No data loaded. Import a PLMXML file first.";
                return;
            }

            var instanceLookup = plmXml.ProductDef.InstanceGraph.ProductInstances
                .ToDictionary(x => x.Id, x => x);

            var viewLookup = plmXml.ProductDef.InstanceGraph.ProductRevisionViews
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.First());

            if (!instanceLookup.TryGetValue(plmXml.ProductDef.InstanceGraph.RootInstanceRef, out var rootInstance))
            {
                Status = "Root instance not found.";
                return;
            }

            var rootNode = BuildNodeRecursive(rootInstance, instanceLookup, viewLookup);
            if (rootNode != null)
            {
                TreeNodes.Add(rootNode);
            }

            int leafCount = _plmXmlService.CurrentLeafNodes?.Count ?? 0;
            Status = $"{TreeNodes.Count} root node(s), {leafCount} leaf nodes";
        }

        private TreeNodeViewModel? BuildNodeRecursive(
            ProductInstance instance,
            Dictionary<string, ProductInstance> instanceLookup,
            Dictionary<string, ProductRevisionView> viewLookup)
        {
            string viewId = instance.PartRef.StartsWith('#')
                ? instance.PartRef.Substring(1)
                : instance.PartRef;

            if (!viewLookup.TryGetValue(viewId, out var view))
                return null;

            bool isLeaf = view.Representation != null;

            var node = new TreeNodeViewModel
            {
                Name = !string.IsNullOrEmpty(instance.Name) ? instance.Name : view.Name,
                Type = isLeaf ? "Part" : "Assembly",
                Id = view.Id,
                FileName = view.Representation?.Location,
                Format = view.Representation?.Format,
                IsLeaf = isLeaf
            };

            if (!string.IsNullOrEmpty(view.InstanceRefs))
            {
                var childIds = view.InstanceRefs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var childId in childIds)
                {
                    if (instanceLookup.TryGetValue(childId, out var childInstance))
                    {
                        var childNode = BuildNodeRecursive(childInstance, instanceLookup, viewLookup);
                        if (childNode != null)
                        {
                            node.Children.Add(childNode);
                        }
                    }
                }
            }

            return node;
        }
    }
}
