namespace PlmXmlSyntax.Models;

public partial class ImportedFileInfo : ObservableObject
{
    public string FullPath { get; init; } = string.Empty;
    public string FileName => System.IO.Path.GetFileName(FullPath);
    
    [ObservableProperty]
    private bool _isProcessed;

    [ObservableProperty]
    private string _status = "Pending";
}
