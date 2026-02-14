using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Win32;
using PlmXmlSyntax.Models;
using PlmXmlSyntax.Services;

namespace PlmXmlSyntax.ViewModels.Pages
{
    public partial class ImporterViewModel : ObservableObject
    {
        private readonly IPlmXmlService _plmXmlService;
        private readonly IPlmXmlAuditService _auditService;

        [ObservableProperty]
        private string _status = "Ready";

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private ObservableCollection<ImportedFileInfo> _stagedFiles = new();

        [ObservableProperty]
        private ObservableCollection<LeafNodeInfo> _leafNodes = new();

        public ImporterViewModel(IPlmXmlService plmXmlService, IPlmXmlAuditService auditService)
        {
            _plmXmlService = plmXmlService;
            _auditService = auditService;
        }

        [RelayCommand]
        private void OnAddFiles()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PLMXML files (*.plmxml)|*.plmxml|XML files (*.xml)|*.xml|All files (*.*)|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var path in openFileDialog.FileNames)
                {
                    if (StagedFiles.All(f => f.FullPath != path))
                    {
                        StagedFiles.Add(new ImportedFileInfo { FullPath = path });
                    }
                }
                UpdateStatus();
            }
        }

        [RelayCommand]
        private void OnRemoveFile(ImportedFileInfo? file)
        {
            if (file != null)
            {
                StagedFiles.Remove(file);
                UpdateStatus();
            }
        }

        [RelayCommand]
        private void OnClearFiles()
        {
            StagedFiles.Clear();
            LeafNodes.Clear();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            if (IsBusy) return;
            Status = StagedFiles.Count == 0 ? "Ready" : $"{StagedFiles.Count} files staged";
        }

        [RelayCommand]
        private async Task OnStartImport()
        {
            if (StagedFiles.Count == 0) return;

            IsBusy = true;
            LeafNodes.Clear();
            int totalNodes = 0;
            var parsedDatasets = new List<(PlmXml Data, string Path)>();

            try
            {
                foreach (var file in StagedFiles)
                {
                    file.IsProcessed = false;
                    file.Status = "Parsing...";
                    Status = $"Processing {file.FileName}...";

                    var plmXml = await _plmXmlService.ParseAsync(file.FullPath);
                    if (plmXml != null)
                    {
                        parsedDatasets.Add((plmXml, file.FullPath));
                        file.Status = "Traversing...";
                        var results = await _plmXmlService.TraverseTreeAsync(plmXml);
                        totalNodes += results.Count;

                        file.Status = "Populating...";
                        const int batchSize = 500;
                        for (int i = 0; i < results.Count; i += batchSize)
                        {
                            var batch = results.Skip(i).Take(batchSize);
                            foreach (var item in batch)
                            {
                                LeafNodes.Add(item);
                            }
                            await Task.Delay(1);
                        }
                        
                        file.IsProcessed = true;
                        file.Status = $"Done ({results.Count} nodes)";
                    }
                    else
                    {
                        file.Status = "Error: Parse failed";
                    }
                }

                // Run Cross-File Analysis
                Status = "Analyzing Collisions...";
                var collisions = await _auditService.AnalyzeCrossFileCollisionsAsync(parsedDatasets);
                if (collisions.Any())
                {
                    var collisionIds = new HashSet<string>(collisions.Select(c => c.NodeId));
                    foreach (var leaf in LeafNodes)
                    {
                        if (collisionIds.Contains(leaf.ProductRevisionViewId))
                        {
                            leaf.Severity = AuditSeverity.Warning;
                            leaf.StatusMessage = $"ID '{leaf.ProductRevisionViewId}' exists in multiple files. Auto-merge will resolve this.";
                        }
                    }
                }

                Status = $"Import Complete \u2014 {totalNodes} nodes total";
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
