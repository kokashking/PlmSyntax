using System.Collections.ObjectModel;

namespace PlmXmlSyntax.Models;

public class TreeNodeViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string? FileName { get; init; }
    public string? Format { get; init; }
    public bool IsLeaf { get; init; }
    public ObservableCollection<TreeNodeViewModel> Children { get; } = new();
}
