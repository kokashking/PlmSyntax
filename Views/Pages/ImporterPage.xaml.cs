using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;
using System.Windows.Data;
using System.ComponentModel;
using System.Linq;
using PlmXmlSyntax.ViewModels.Pages;
using PlmXmlSyntax.Models;
using Wpf.Ui.Controls;
using Wpf.Ui.Abstractions.Controls;

namespace PlmXmlSyntax.Views.Pages
{
    public class PathToFileNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                return System.IO.Path.GetFileName(path);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public partial class ImporterPage : INavigableView<ImporterViewModel>
    {
        public ImporterViewModel ViewModel { get; }

        public ImporterPage(ImporterViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            Loaded += (s, e) => 
            {
                var parentScroll = GetParentScrollViewer(this);
                if (parentScroll != null)
                {
                    parentScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                }
            };
        }

        private void OnStagedFilesSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.ListView listView)
            {
                var selectedPaths = listView.SelectedItems
                    .Cast<ImportedFileInfo>()
                    .Select(f => f.FullPath)
                    .ToList();
                
                ViewModel.UpdateFilter(selectedPaths);
            }
        }

        private ScrollViewer? GetParentScrollViewer(DependencyObject obj)
        {
            var parent = VisualTreeHelper.GetParent(obj);
            if (parent == null) return null;
            if (parent is ScrollViewer sv) return sv;
            return GetParentScrollViewer(parent);
        }

        private void ListView_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                var scv = GetScrollViewer(sender as DependencyObject);
                if (scv != null)
                {
                    if (e.Delta > 0)
                        scv.LineUp();
                    else
                        scv.LineDown();

                    e.Handled = true;
                }
            }
        }

        private GridViewColumnHeader? _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private void OnColumnHeaderClick(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked == null || headerClicked.Role == GridViewColumnHeaderRole.Padding) return;

            ListSortDirection direction;
            if (headerClicked != _lastHeaderClicked)
            {
                direction = ListSortDirection.Ascending;
            }
            else
            {
                direction = _lastDirection == ListSortDirection.Ascending 
                    ? ListSortDirection.Descending 
                    : ListSortDirection.Ascending;
            }

            var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
            var sortBy = columnBinding?.Path.Path ?? GetPropertyName(headerClicked.Column.Header as string);

            if (!string.IsNullOrEmpty(sortBy))
            {
                Sort(sortBy, direction);
                _lastHeaderClicked = headerClicked;
                _lastDirection = direction;
            }
        }

        private string GetPropertyName(string? header)
        {
            return header switch
            {
                "Status" => "Severity",
                "Source" => "SourcePlmXmlPath",
                "Name" => "Name",
                "File" => "FileName",
                "Format" => "Format",
                "ID" => "ProductRevisionViewId",
                "Part Numbers" => "PartNumbers",
                _ => string.Empty
            };
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            var dataView = CollectionViewSource.GetDefaultView(LeafNodesList.ItemsSource);
            if (dataView == null) return;

            var selectedItem = LeafNodesList.SelectedItem;

            dataView.SortDescriptions.Clear();
            SortDescription sd = new SortDescription(sortBy, direction);
            dataView.SortDescriptions.Add(sd);
            
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (selectedItem != null)
                {
                    LeafNodesList.ScrollIntoView(selectedItem);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private ScrollViewer? GetScrollViewer(DependencyObject? o)
        {
            if (o is ScrollViewer viewer) return viewer;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = VisualTreeHelper.GetChild(o, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
