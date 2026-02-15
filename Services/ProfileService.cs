using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PlmXmlSyntax.Models;

namespace PlmXmlSyntax.Services;

public class ProfileService : IProfileService
{
    private readonly string _profilesDir;
    private List<ParsingProfile> _profiles = new();
    private string _activeProfileName = "Default";

    public ProfileService()
    {
        _profilesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
        if (!Directory.Exists(_profilesDir))
        {
            Directory.CreateDirectory(_profilesDir);
        }
        LoadAll();
    }

    private void LoadAll()
    {
        _profiles.Clear();
        foreach (var file in Directory.GetFiles(_profilesDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<ParsingProfile>(json);
                if (profile != null) _profiles.Add(profile);
            }
            catch { /* Log error */ }
        }

        if (!_profiles.Any(p => p.Name == "Default"))
        {
            var defaultProfile = ParsingProfile.Default;
            SaveProfile(defaultProfile);
        }
    }

    public IEnumerable<ParsingProfile> GetAllProfiles() => _profiles;

    public ParsingProfile GetActiveProfile() => 
        _profiles.FirstOrDefault(p => p.Name == _activeProfileName) ?? _profiles.First();

    public void SetActiveProfile(string name) => _activeProfileName = name;

    public void SaveProfile(ParsingProfile profile)
    {
        var existing = _profiles.FirstOrDefault(p => p.Name == profile.Name);
        if (existing != null) _profiles.Remove(existing);
        _profiles.Add(profile);

        var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(_profilesDir, $"{profile.Name}.json"), json);
    }

    public void DeleteProfile(string name)
    {
        if (name == "Default") return;
        var profile = _profiles.FirstOrDefault(p => p.Name == name);
        if (profile != null)
        {
            _profiles.Remove(profile);
            var path = Path.Combine(_profilesDir, $"{name}.json");
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
