using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using X52.CustomDriver.Core.Models;

namespace X52.CustomDriver.Core.Services
{
    public class ProfileService
    {
        private readonly string _profilesPath;
        private List<X52Profile> _profiles = new();
        private X52Profile _activeProfile = new();
        private CancellationTokenSource? _ccts;

        public event EventHandler<X52Profile>? OnProfileChanged;
        public IReadOnlyList<X52Profile> Profiles => _profiles;
        public X52Profile ActiveProfile => _activeProfile;

        public ProfileService()
        {
            _profilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles.json");
            LoadProfiles();
            
            if (!_profiles.Any())
            {
                CreateDefaultProfiles();
            }
            
            _activeProfile = _profiles.First(p => p.Name == "Default");
        }

        private void LoadProfiles()
        {
            try
            {
                if (File.Exists(_profilesPath))
                {
                    string json = File.ReadAllText(_profilesPath);
                    _profiles = JsonSerializer.Deserialize<List<X52Profile>>(json) ?? new List<X52Profile>();
                }
            }
            catch { _profiles = new List<X52Profile>(); }
        }

        public void SaveProfiles()
        {
            try
            {
                string json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_profilesPath, json);
            }
            catch (Exception ex) { Console.WriteLine($"[ERROR] Failed to save profiles: {ex.Message}"); }
        }

        private void CreateDefaultProfiles()
        {
            _profiles.Add(new X52Profile { Name = "Default" });
            _profiles.Add(new X52Profile { 
                Name = "MSFS 2020", 
                ProcessName = "FlightSimulator",
                AxisSettings = new AxisSettings { SensitivityX = 0.8, SensitivityY = 0.8 }
            });
            _profiles.Add(new X52Profile { 
                Name = "DCS World", 
                ProcessName = "DCS",
                Mappings = new ObservableCollection<ButtonMapping> {
                    new ButtonMapping { ButtonName = "ButtonD", KeySequence = new List<string>{"LSHIFT", "G"} }
                }
            });
            SaveProfiles();
        }

        public void AddProfile(X52Profile profile)
        {
            if (_profiles.Any(p => p.Name == profile.Name)) return;
            _profiles.Add(profile);
            SaveProfiles();
        }

        public void RemoveProfile(X52Profile profile)
        {
            if (_profiles.Count <= 1) return; // Don't delete the last profile
            _profiles.Remove(profile);
            SaveProfiles();
        }

        public void UpdateProfile(X52Profile profile)
        {
            var index = _profiles.FindIndex(p => p.Name == profile.Name);
            if (index != -1)
            {
                _profiles[index] = profile;
                if (_activeProfile.Name == profile.Name)
                {
                    _activeProfile = profile;
                    OnProfileChanged?.Invoke(this, _activeProfile);
                }
                SaveProfiles();
            }
        }

        public void StartWatcher()
        {
            _ccts = new CancellationTokenSource();
            Task.Run(() => WatchLoop(_ccts.Token));
        }

        public void StopWatcher() => _ccts?.Cancel();

        private async Task WatchLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var runningProcesses = Process.GetProcesses().Select(p => p.ProcessName).ToList();
                    
                    var matchedProfile = _profiles.FirstOrDefault(p => 
                        !string.IsNullOrEmpty(p.ProcessName) && 
                        runningProcesses.Contains(p.ProcessName, StringComparer.OrdinalIgnoreCase));

                    var targetProfile = matchedProfile ?? _profiles.First(p => p.Name == "Default");

                    if (targetProfile.Name != _activeProfile.Name)
                    {
                        _activeProfile = targetProfile;
                        OnProfileChanged?.Invoke(this, _activeProfile);
                    }
                }
                catch { }

                await Task.Delay(5000, token);
            }
        }
    }
}
