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

        // TODO: Remove auto-import after debugging
        private const string DebugAutoImportPath = @"C:\Projects\Pope\E4 upload docs\HDBT_PVS_PO546_BASIS_LL_BRD_1JT.plmxml";

        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private string _status = "Ready";

        [ObservableProperty]
        private bool _isBusy = false;

        [ObservableProperty]
        private ObservableCollection<LeafNodeInfo> _leafNodes = new();

        public ImporterViewModel(IPlmXmlService plmXmlService)
        {
            _plmXmlService = plmXmlService;

            if (File.Exists(DebugAutoImportPath))
                _ = AutoImportAsync();
        }

        private async Task AutoImportAsync()
        {
            await Task.Delay(500);
            FilePath = DebugAutoImportPath;
            await RunImportAsync();
        }

        [RelayCommand]
        private async Task OnImportFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PLMXML files (*.plmxml)|*.plmxml|XML files (*.xml)|*.xml|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FilePath = openFileDialog.FileName;
                await RunImportAsync();
            }
        }

        private async Task RunImportAsync()
        {
            Status = "Parsing...";
            IsBusy = true;
            LeafNodes.Clear();

            try
            {
                var plmXml = await _plmXmlService.ParseAsync(FilePath);
                if (plmXml != null)
                {
                    Status = "Traversing tree...";
                    var results = await _plmXmlService.TraverseTreeAsync(plmXml);

                    Status = $"Populating UI \u2014 {results.Count} nodes...";
                    
                    // Chunked loading to keep UI responsive
                    const int batchSize = 500;
                    for (int i = 0; i < results.Count; i += batchSize)
                    {
                        var batch = results.Skip(i).Take(batchSize);
                        foreach (var item in batch)
                        {
                            LeafNodes.Add(item);
                        }
                        
                        // Give the UI thread a moment to process the new items and remain responsive
                        await Task.Delay(1); 
                        
                        if (IsBusy == false) break; // Allow cancellation if we implement it later
                    }

                    Status = $"Complete \u2014 {results.Count} leaf nodes found";
                }
                else
                {
                    Status = "Failed to parse file";
                }
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
