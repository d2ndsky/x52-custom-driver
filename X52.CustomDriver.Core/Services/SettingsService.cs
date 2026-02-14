using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using X52.CustomDriver.Core.Models;

namespace X52.CustomDriver.Core.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;
        private AppSettings _settings = new();

        public AppSettings CurrentSettings => _settings;

        public SettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appData, "AerakonX52Driver");
            if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);
            _settingsPath = Path.Combine(appFolder, "settings.json");
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    string json = File.ReadAllText(_settingsPath);
                    _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { _settings = new AppSettings(); }
            
            // Sync registry state in case user changed it externally
            // But actually, we want the AppSettings to drive the Registry.
            // If we are starting up, maybe we trust the file.
        }

        public void SaveSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
                ApplyStartupSetting();
            }
            catch (Exception ex) { Console.WriteLine($"[ERROR] Failed to save settings: {ex.Message}"); }
        }

        private void ApplyStartupSetting()
        {
            try
            {
                string keyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyName, true))
                {
                    if (key == null) return;

                    string appName = "AerakonX52Driver";
                    if (_settings.RunAtStartup)
                    {
                        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
                        if (!string.IsNullOrEmpty(exePath))
                        {
                             key.SetValue(appName, $"\"{exePath}\"");
                        }
                    }
                    else
                    {
                        if (key.GetValue(appName) != null)
                            key.DeleteValue(appName);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to set startup registry key: {ex.Message}");
            }
        }
    }
}
