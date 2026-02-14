using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace X52.CustomDriver.Core.Models
{
    public class X52Profile
    {
        public string Name { get; set; } = "Default";
        public string? ProcessName { get; set; } // e.g. "DCS", "FlightSimulator"
        public ObservableCollection<ButtonMapping> Mappings { get; set; } = new();
        public AxisSettings AxisSettings { get; set; } = new();
    }

    public class ButtonMapping
    {
        public string ButtonName { get; set; } = ""; // e.g. "Trigger", "ButtonD"
        public bool EnableVJoy { get; set; } = true;
        public List<string>? KeySequence { get; set; } // e.g. ["LSHIFT", "G"]
        public bool IsToggle { get; set; } = false;
        public int Mode { get; set; } = 0; // 0 = All, 1, 2, 3

        [System.Text.Json.Serialization.JsonIgnore]
        public string KeySequenceString
        {
            get => KeySequence != null ? string.Join("+", KeySequence) : "";
            set
            {
                if (string.IsNullOrWhiteSpace(value)) KeySequence = null;
                else KeySequence = new List<string>(value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }
    }

    public class AxisSettings
    {
        public double SensitivityX { get; set; } = 1.0;
        public double SensitivityY { get; set; } = 1.0;
        public double DeadzoneZ { get; set; } = 40;
        public bool InvertThrottle { get; set; } = false;
    }
}
