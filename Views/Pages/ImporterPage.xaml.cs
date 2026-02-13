using System.Globalization;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Media;
using PlmXmlSyntax.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace PlmXmlSyntax.Views.Pages
{
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

    public class ListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<string> list)
                return string.Join(", ", list.Where(s => !string.IsNullOrEmpty(s)));
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
