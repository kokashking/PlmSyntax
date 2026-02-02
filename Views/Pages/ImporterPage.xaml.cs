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
}
