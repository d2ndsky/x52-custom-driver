using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using X52.CustomDriver.App.ViewModels;
using X52.CustomDriver.Core.Models;

namespace X52.CustomDriver.App
{
    public partial class MappingEditorWindow : Window
    {
        private X52ViewModel _viewModel;

        public List<string> AvailableButtons { get; } = new()
        {
            "Trigger", "TriggerStage2", "Pinkie", "ButtonFire", "ButtonA", "ButtonB", "ButtonC", "ButtonD", "ButtonE",
            "T1", "T2", "T3", "T4", "T5", "T6",
            "Hat1Up", "Hat1Down", "Hat1Left", "Hat1Right",
            "Hat2Up", "Hat2Down", "Hat2Left", "Hat2Right",
            "HatRearUp", "HatRearDown", "HatRearLeft", "HatRearRight",
            "Rotary1Min", "Rotary1Max", "Rotary2Min", "Rotary2Max", "SliderMin", "SliderMax",
            "MfdFunction", "MfdStartStop", "MfdReset", "ClutchButton",
            "MouseLeftClick", "MouseNubClick", "MouseWheelClick", "MouseWheelUp", "MouseWheelDown"
        };

        public List<int> AvailableModes { get; } = new() { 0, 1, 2, 3 };

        public MappingEditorWindow(X52ViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = this;

            // Load profiles
            ProfilesList.ItemsSource = _viewModel.ProfileService.Profiles;
            if (_viewModel.ProfileService.Profiles.Count > 0)
            {
                ProfilesList.SelectedItem = _viewModel.CurrentProfile;
            }
        }

        private void ProfilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfilesList.SelectedItem is X52Profile profile)
            {
                _viewModel.CurrentProfile = profile;
                MappingsGrid.ItemsSource = null;
                MappingsGrid.ItemsSource = profile.Mappings;
                
                ProcessNameBox.Text = profile.ProcessName;
                SensitivityXBox.Text = profile.AxisSettings.SensitivityX.ToString("0.0");
                SensitivityYBox.Text = profile.AxisSettings.SensitivityY.ToString("0.0");
            }
        }

        private void NewProfile_Click(object sender, RoutedEventArgs e)
        {
            var newProfile = new X52Profile { Name = "New Profile " + DateTime.Now.ToString("HHmm") };
            _viewModel.ProfileService.AddProfile(newProfile);
            ProfilesList.Items.Refresh();
            ProfilesList.SelectedItem = newProfile;
        }

        private void AddMapping_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentProfile == null)
            {
                System.Windows.MessageBox.Show("No profile selected.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }
            
            var newMapping = new ButtonMapping { ButtonName = "Trigger", KeySequence = new List<string> { "SPACE" } };
            _viewModel.CurrentProfile.Mappings.Add(newMapping);
            
            // Force re-bind to be absolutely sure the change is picked up
            MappingsGrid.ItemsSource = null;
            MappingsGrid.ItemsSource = _viewModel.CurrentProfile.Mappings;
        }

        private void RemoveMapping_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentProfile == null) return;
            
            if (MappingsGrid.SelectedItem is ButtonMapping selectedMapping)
            {
                _viewModel.CurrentProfile.Mappings.Remove(selectedMapping);
                
                // Force re-bind
                MappingsGrid.ItemsSource = null;
                MappingsGrid.ItemsSource = _viewModel.CurrentProfile.Mappings;
            }
            else
            {
                System.Windows.MessageBox.Show("Select a mapping to remove.", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentProfile == null) return;
            // TODO: Implement deletion confirmation
            _viewModel.ProfileService.RemoveProfile(_viewModel.CurrentProfile);
            ProfilesList.Items.Refresh();
            if (_viewModel.ProfileService.Profiles.Count > 0)
                ProfilesList.SelectedIndex = 0;
            else
                MappingsGrid.ItemsSource = null;
        }

        private void SaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentProfile != null)
            {
                _viewModel.CurrentProfile.ProcessName = ProcessNameBox.Text;
                if (double.TryParse(SensitivityXBox.Text, out double sensX))
                {
                    _viewModel.CurrentProfile.AxisSettings.SensitivityX = sensX;
                }
                if (double.TryParse(SensitivityYBox.Text, out double sensY))
                {
                    _viewModel.CurrentProfile.AxisSettings.SensitivityY = sensY;
                }
            }
            _viewModel.ProfileService.SaveProfiles();
            System.Windows.MessageBox.Show("Profiles saved to disk.", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        // --- MACRO RECORDING LOGIC ---

        private void MacroBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;

            e.Handled = true; // Prevent default typing

            // Handle special editing keys
            if (e.Key == System.Windows.Input.Key.Delete)
            {
                textBox.Text = "";
                UpdateBinding(textBox);
                return;
            }
            
            if (e.Key == System.Windows.Input.Key.Back)
            {
                // Remove last token
                string current = textBox.Text;
                int lastPlus = current.LastIndexOf('+');
                if (lastPlus >= 0)
                {
                    textBox.Text = current.Substring(0, lastPlus);
                }
                else
                {
                    textBox.Text = "";
                }
                UpdateBinding(textBox);
                return;
            }

            // Ignore repeats to prevent flooding
            if (e.IsRepeat) return;

            // Get string representation
            string keyStr = GetKeyString(e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key);
            if (string.IsNullOrEmpty(keyStr)) return;

            // Reset text if it was fully selected (user wants to replace)
            if (textBox.SelectedText == textBox.Text && !string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = "";
            }

            string separator = string.IsNullOrEmpty(textBox.Text) ? "" : "+";
            
            // Avoid adding duplicate modifiers consecutively if holding down
            if (!string.IsNullOrEmpty(textBox.Text))
            {
                string[] parts = textBox.Text.Split('+');
                string lastKey = parts[parts.Length - 1];
                if (lastKey == keyStr) return; // Don't repeat "LCTRL+LCTRL"
            }

            textBox.Text += separator + keyStr;
            textBox.CaretIndex = textBox.Text.Length; // Move caret to end
            UpdateBinding(textBox);
        }

        private void UpdateBinding(System.Windows.Controls.TextBox textBox)
        {
            BindingExpression be = textBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty);
            be?.UpdateSource();
        }

        private string GetKeyString(System.Windows.Input.Key k)
        {
            // Map WPF keys to our internal format
            switch (k)
            {
                case System.Windows.Input.Key.LeftCtrl: return "LCTRL";
                case System.Windows.Input.Key.RightCtrl: return "RCTRL";
                case System.Windows.Input.Key.LeftShift: return "LSHIFT";
                case System.Windows.Input.Key.RightShift: return "RSHIFT";
                case System.Windows.Input.Key.LeftAlt: return "LALT";
                case System.Windows.Input.Key.RightAlt: return "RALT";
                case System.Windows.Input.Key.LWin: return "LWIN";
                case System.Windows.Input.Key.RWin: return "RWIN";
                case System.Windows.Input.Key.Enter: return "ENTER";
                case System.Windows.Input.Key.Space: return "SPACE";
                case System.Windows.Input.Key.Tab: return "TAB";
                case System.Windows.Input.Key.Escape: return "ESCAPE";
                case System.Windows.Input.Key.Back: return "BACKSPACE";
                case System.Windows.Input.Key.Delete: return "DELETE";
                case System.Windows.Input.Key.Up: return "UP";
                case System.Windows.Input.Key.Down: return "DOWN";
                case System.Windows.Input.Key.Left: return "LEFT";
                case System.Windows.Input.Key.Right: return "RIGHT";
                case System.Windows.Input.Key.D0: return "0";
                case System.Windows.Input.Key.D1: return "1";
                case System.Windows.Input.Key.D2: return "2";
                case System.Windows.Input.Key.D3: return "3";
                case System.Windows.Input.Key.D4: return "4";
                case System.Windows.Input.Key.D5: return "5";
                case System.Windows.Input.Key.D6: return "6";
                case System.Windows.Input.Key.D7: return "7";
                case System.Windows.Input.Key.D8: return "8";
                case System.Windows.Input.Key.D9: return "9";
            }

            if (k >= System.Windows.Input.Key.A && k <= System.Windows.Input.Key.Z) return k.ToString();
            if (k >= System.Windows.Input.Key.F1 && k <= System.Windows.Input.Key.F12) return k.ToString();
            
            // NumPad
            if (k >= System.Windows.Input.Key.NumPad0 && k <= System.Windows.Input.Key.NumPad9) return k.ToString().Replace("NumPad", "");

            return ""; // Unknown key or not mapped
        }
    }

    public class ModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int mode)
            {
                return mode == 0 ? "Any Mode" : $"Mode {mode}";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return 0; // Not used
        }
    }
}
