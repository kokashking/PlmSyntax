using System.Globalization;
using System.Windows.Data;
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
