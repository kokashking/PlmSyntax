using Microsoft.Win32;
using PlmXmlSyntax.Services;
using Wpf.Ui.Controls;

namespace PlmXmlSyntax.ViewModels.Pages
{
    public partial class ImporterViewModel : ObservableObject
    {
        private readonly IPlmXmlService _plmXmlService;

        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private string _status = "Ready";

        [ObservableProperty]
        private bool _isBusy = false;

        public ImporterViewModel(IPlmXmlService plmXmlService)
        {
            _plmXmlService = plmXmlService;
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
                Status = "Parsing...";
                IsBusy = true;

                try
                {
                    var plmXml = await _plmXmlService.ParseAsync(FilePath);
                    if (plmXml != null)
                    {
                        Status = "Traversing tree...";
                        await _plmXmlService.TraverseTreeAsync(plmXml);
                        Status = "Import Complete";
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
}