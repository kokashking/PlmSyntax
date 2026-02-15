using System.Collections.ObjectModel;
using PlmXmlSyntax.Models;
using PlmXmlSyntax.Services;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace PlmXmlSyntax.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private readonly IProfileService _profileService;
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = String.Empty;

        [ObservableProperty]
        private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

        [ObservableProperty]
        private ObservableCollection<ParsingProfile> _profiles = new();

        [ObservableProperty]
        private ParsingProfile? _selectedProfile;

        public Array MergeStrategies => Enum.GetValues(typeof(MergeStrategy));
        public Array RootSearchStrategies => Enum.GetValues(typeof(RootSearchStrategy));

        public SettingsViewModel(IProfileService profileService)
        {
            _profileService = profileService;
        }

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            CurrentTheme = ApplicationThemeManager.GetAppTheme();
            AppVersion = $"PLM Syntax - {GetAssemblyVersion()}";

            LoadProfiles();

            _isInitialized = true;
        }

        private void LoadProfiles()
        {
            Profiles = new ObservableCollection<ParsingProfile>(_profileService.GetAllProfiles());
            SelectedProfile = _profileService.GetActiveProfile();
        }

        [RelayCommand]
        private void OnSaveProfile()
        {
            if (SelectedProfile != null)
            {
                _profileService.SaveProfile(SelectedProfile);
                _profileService.SetActiveProfile(SelectedProfile.Name);
            }
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        [RelayCommand]
        private void OnChangeTheme(string parameter)
        {
            // Existing theme logic...
            switch (parameter)
            {
                case "theme_light":
                    if (CurrentTheme == ApplicationTheme.Light) break;
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    CurrentTheme = ApplicationTheme.Light;
                    break;
                default:
                    if (CurrentTheme == ApplicationTheme.Dark) break;
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    CurrentTheme = ApplicationTheme.Dark;
                    break;
            }
        }
    }
}
