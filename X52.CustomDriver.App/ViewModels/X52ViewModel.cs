using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using X52.CustomDriver.Core.Interfaces;
using X52.CustomDriver.Core.Models;
using X52.CustomDriver.Core.Services;

namespace X52.CustomDriver.App.ViewModels
{
    public class X52ViewModel : INotifyPropertyChanged
    {
        private readonly IHidService _hidService;
        private readonly IVJoyService _vJoyService;
        private readonly KeyboardService _keyboardService = new();
        private readonly ProfileService _profileService;
        private readonly SettingsService _settingsService;
        public ProfileService ProfileService => _profileService;
        public SettingsService SettingsService => _settingsService;

        private X52State _state = new();
        private X52State _prevState = new();
        private X52Profile _currentProfile = new();

        public X52State State
        {
            get => _state;
            set { _prevState = _state; _state = value; OnPropertyChanged(); }
        }
        
        // Settings Properties
        public bool MinimizeToTray
        {
            get => _settingsService.CurrentSettings.MinimizeToTray;
            set 
            { 
                _settingsService.CurrentSettings.MinimizeToTray = value; 
                _settingsService.SaveSettings(); 
                OnPropertyChanged(); 
            }
        }

        public bool CloseToTray
        {
            get => _settingsService.CurrentSettings.CloseToTray;
            set 
            { 
                _settingsService.CurrentSettings.CloseToTray = value; 
                _settingsService.SaveSettings(); 
                OnPropertyChanged(); 
            }
        }

        public bool RunAtStartup
        {
            get => _settingsService.CurrentSettings.RunAtStartup;
            set 
            { 
                _settingsService.CurrentSettings.RunAtStartup = value; 
                _settingsService.SaveSettings(); 
                OnPropertyChanged(); 
            }
        }

        public double SensitivityX
        {
            get => CurrentProfile.AxisSettings.SensitivityX;
            set
            {
                CurrentProfile.AxisSettings.SensitivityX = value;
                OnPropertyChanged();
                _profileService.SaveProfiles();
            }
        }

        public double SensitivityY
        {
            get => CurrentProfile.AxisSettings.SensitivityY;
            set
            {
                CurrentProfile.AxisSettings.SensitivityY = value;
                OnPropertyChanged();
                _profileService.SaveProfiles();
            }
        }

        public X52Profile CurrentProfile
        {
            get => _currentProfile;
            set 
            { 
                _currentProfile = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(ProfileName));
                OnPropertyChanged(nameof(SensitivityX));
                OnPropertyChanged(nameof(SensitivityY));
            }
        }

        public string ProfileName => CurrentProfile.Name;

        public string RawDataString => State.RawData != null ? BitConverter.ToString(State.RawData).Replace("-", " ") : "No Data";

        public double XPercent => (State.X / 2048.0) * 100;
        public double YPercent => (State.Y / 2048.0) * 100;
        public double ZPercent => (State.Z / 1024.0) * 100;
        public double ThrottlePercent => (State.Throttle / 255.0) * 100;
        public double Rotary1Percent => (State.Rotary1 / 255.0) * 100;
        public double Rotary2Percent => (State.Rotary2 / 255.0) * 100;
        public double SliderPercent => (State.Slider / 255.0) * 100;

        public bool IsConnected => _hidService.IsConnected;
        public bool IsVJoyActive => _vJoyService.IsAvailable;

        public X52ViewModel(IHidService hidService, IVJoyService vJoyService, ProfileService profileService, SettingsService settingsService)
        {
            _hidService = hidService;
            _vJoyService = vJoyService;
            _profileService = profileService;
            _settingsService = settingsService;

            InitializeButtons();

            _profileService.OnProfileChanged += (s, p) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentProfile = p;
                });
            };

            _hidService.OnStateChanged += (s, e) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    State = e;
                    UpdatePhysicalButtons(e);
                    ProcessKeyMappings();
                    
                    OnPropertyChanged(nameof(XPercent));
                    OnPropertyChanged(nameof(YPercent));
                    OnPropertyChanged(nameof(ZPercent));
                    OnPropertyChanged(nameof(ThrottlePercent));
                    OnPropertyChanged(nameof(Rotary1Percent));
                    OnPropertyChanged(nameof(Rotary2Percent));
                    OnPropertyChanged(nameof(SliderPercent));
                    OnPropertyChanged(nameof(IsConnected));
                    OnPropertyChanged(nameof(IsVJoyActive));
                    OnPropertyChanged(nameof(RawDataString));
                    OnPropertyChanged(nameof(IsMode1));
                    OnPropertyChanged(nameof(IsMode2));
                    OnPropertyChanged(nameof(IsMode3));
                });

                UpdateVJoy(e);
            };
        }

        public bool IsMode1 => State.CurrentMode == 1;
        public bool IsMode2 => State.CurrentMode == 2;
        public bool IsMode3 => State.CurrentMode == 3;

        public System.Collections.ObjectModel.ObservableCollection<ButtonVisualState> PhysicalButtons { get; } = new();

        private readonly string[] _buttonNames = { 
            "Trigger", "ButtonFire", "ButtonA", "ButtonB", "ButtonC", "Pinkie", "ButtonD", "ButtonE", 
            "T1", "T2", "T3", "T4", "T5", "T6", "TriggerStage2",
            "Hat1Up", "Hat1Right", "Hat1Down", "Hat1Left",
            "HatRearUp", "HatRearRight", "HatRearDown", "HatRearLeft",
            "MfdFunction", "MfdStartStop", "MfdReset", "ClutchButton", "MouseLeftClick",
            "Hat2Up", "Hat2Down", "Hat2Left", "Hat2Right"
        };

        private void InitializeButtons()
        {
            for (int i = 0; i < 32; i++)
            {
                PhysicalButtons.Add(new ButtonVisualState { Name = (i + 1).ToString() });
            }
        }

        private void UpdatePhysicalButtons(X52State s)
        {
            for (int i = 0; i < _buttonNames.Length && i < PhysicalButtons.Count; i++)
            {
                PhysicalButtons[i].IsPressed = GetButtonState(s, _buttonNames[i]);
            }
        }

        private void ProcessKeyMappings()
        {
            foreach (var mapping in CurrentProfile.Mappings)
            {
                // Check Mode (0 = All modes, else must match CurrentMode)
                if (mapping.Mode != 0 && mapping.Mode != State.CurrentMode)
                    continue;

                bool currentState = GetButtonState(State, mapping.ButtonName);
                bool prevState = GetButtonState(_prevState, mapping.ButtonName);

                if (currentState && !prevState) // On Press
                {
                    if (mapping.KeySequence != null)
                        _keyboardService.SendKeys(mapping.KeySequence);
                }
            }
        }

        private bool GetButtonState(X52State state, string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            var prop = typeof(X52State).GetProperty(name);
            return (bool)(prop?.GetValue(state) ?? false);
        }

        private void UpdateVJoy(X52State s)
        {
            if (!_vJoyService.IsAvailable) return;
            
            // Apply Sensitivity
            double sensX = CurrentProfile.AxisSettings.SensitivityX;
            double sensY = CurrentProfile.AxisSettings.SensitivityY;
            
            int cx = 1024;
            double rawX = (s.X - cx) * sensX + cx;
            double rawY = (s.Y - cx) * sensY + cx;
            
            int finalX = Math.Clamp((int)rawX, 0, 2048);
            int finalY = Math.Clamp((int)rawY, 0, 2048);

            _vJoyService.SetAxisX(finalX * 16);
            _vJoyService.SetAxisY(finalY * 16);
            _vJoyService.SetAxisZ(s.Z * 32);
            
            _vJoyService.SetRx(s.Throttle * 128);
            _vJoyService.SetRy(s.Rotary1 * 128);
            _vJoyService.SetRz(s.Rotary2 * 128);
            _vJoyService.SetSlider(s.Slider * 128);
            
            // --- GHOSTBUSTER LOGIC (Ported from Console) ---
            if (s.RawData != null)
            {
                 int vBtn = 1 + (s.CurrentMode - 1) * 32;
                 
                 // Limit to B11 to avoid mapping Raw Hat 2 (B12) and Nub Noise (B13) to buttons 33-48
                 for (int b = 8; b < Math.Min(s.RawData.Length, 12); b++)
                 {
                     for (int bit = 0; bit < 8; bit++)
                     {
                         if (vBtn <= 128)
                             _vJoyService.SetButton(vBtn++, (s.RawData[b] & (1 << bit)) != 0);
                     }
                 }

                 // SILVER BULLET PHASE 2: Explicit Clean Mapping for Hat 2
                 // We override buttons 29-32 (which might have been 0'd by B11 upper nibble)
                 // baseBtn is 1. +28 -> 29.
                 int baseId = 1 + (s.CurrentMode - 1) * 32;
                 _vJoyService.SetButton(baseId + 28, s.Hat2Up);    // Button 29
                 _vJoyService.SetButton(baseId + 29, s.Hat2Down);  // Button 30
                 _vJoyService.SetButton(baseId + 30, s.Hat2Left);  // Button 31
                 _vJoyService.SetButton(baseId + 31, s.Hat2Right); // Button 32
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void CalibrateCenter() => _hidService.CalibrateCenter();
        public void ResetCalibration() => _hidService.ResetCalibration();
    }

    public class ButtonVisualState : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private bool _isPressed;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public bool IsPressed
        {
            get => _isPressed;
            set { _isPressed = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
