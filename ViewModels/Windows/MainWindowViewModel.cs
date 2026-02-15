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
                Icon = new SymbolIcon { Symbol = SymbolRegular.ArrowImport24, FontSize = 16 },
                TargetPageType = typeof(Views.Pages.ImporterPage),
                MinHeight = 32,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 1, 0, 1),
                FontSize = 13
            },
            new NavigationViewItem()
            {
                Content = "Explorer",
                Icon = new SymbolIcon { Symbol = SymbolRegular.TreeDeciduous20, FontSize = 16 },
                TargetPageType = typeof(Views.Pages.DataPage),
                MinHeight = 32,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 1, 0, 1),
                FontSize = 13
            }
        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24, FontSize = 16 },
                TargetPageType = typeof(Views.Pages.SettingsPage),
                MinHeight = 32,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 1, 0, 1),
                FontSize = 13
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Importer", Tag = "tray_home" }
        };
    }
}
