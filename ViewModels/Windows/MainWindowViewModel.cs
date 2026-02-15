using System.Collections.ObjectModel;
using System.Windows;
using Wpf.Ui.Controls;

namespace PlmXmlSyntax.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _applicationTitle = "PlmSyntax";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Importer",
                Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowImport20, FontSize = 14 },
                TargetPageType = typeof(Views.Pages.ImporterPage),
                MinHeight = 28,
                Padding = new Thickness(4, 2, 4, 2),
                Margin = new Thickness(0, 0, 0, 0),
                FontSize = 12
            },
            new NavigationViewItem()
            {
                Content = "Explorer",
                Icon = new SymbolIcon { Symbol = SymbolRegular.TreeDeciduous20, FontSize = 14 },
                TargetPageType = typeof(Views.Pages.DataPage),
                MinHeight = 28,
                Padding = new Thickness(4, 2, 4, 2),
                Margin = new Thickness(0, 0, 0, 0),
                FontSize = 12
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings20, FontSize = 14 },
                TargetPageType = typeof(Views.Pages.SettingsPage),
                MinHeight = 28,
                Padding = new Thickness(4, 2, 4, 2),
                Margin = new Thickness(0, 0, 0, 0),
                FontSize = 12
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Importer", Tag = "tray_home" }
        };
    }
}
