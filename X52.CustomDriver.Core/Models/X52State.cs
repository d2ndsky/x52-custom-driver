using System;

namespace X52.CustomDriver.Core.Models
{
    public class X52State
    {
        // Axes (0-1023 approx raw range, normalized later)
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; } // Twist (Timón)
        public int Throttle { get; set; } // Gases
        
        // Rotaries and Slider
        // Rotaries and Slider (Analog + Virtual Buttons)
        public int Rotary1 { get; set; }
        public int Rotary2 { get; set; }
        public int Slider { get; set; }

        public bool Rotary1Max => Rotary1 > 250;
        public bool Rotary1Min => Rotary1 < 5;
        public bool Rotary2Max => Rotary2 > 250;
        public bool Rotary2Min => Rotary2 < 5;
        public bool SliderMax => Slider > 250;
        public bool SliderMin => Slider < 5;
        
        // Mode Selector (1, 2, 3)
        public int CurrentMode { get; set; } = 1;

        public bool Trigger { get; set; }
        public bool TriggerStage2 { get; set; }
        public bool Pinkie { get; set; }
        public bool ButtonFire { get; set; }
        public bool ButtonA { get; set; }
        public bool ButtonB { get; set; }
        public bool ButtonC { get; set; }
        
        // Hat 1 (Seta 1)
        public bool Hat1Up { get; set; }
        public bool Hat1Down { get; set; }
        public bool Hat1Left { get; set; }
        public bool Hat1Right { get; set; }

        // Hat 2 (Seta 2 - Throttle)
        public bool Hat2Up { get; set; }
        public bool Hat2Down { get; set; }
        public bool Hat2Left { get; set; }
        public bool Hat2Right { get; set; }

        // Joystick Base
        public bool T1 { get; set; }
        public bool T2 { get; set; }
        public bool T3 { get; set; }
        public bool T4 { get; set; }
        public bool T5 { get; set; }
        public bool T6 { get; set; }

        // Throttle Base
        public bool MfdFunction { get; set; }
        public bool MfdStartStop { get; set; }
        public bool MfdReset { get; set; }
        public bool ClutchButton { get; set; }

        public bool ButtonD { get; set; }
        public bool ButtonE { get; set; }
        public bool MouseLeftClick { get; set; }
        public bool MouseNubClick { get; set; }
        public bool MouseWheelClick { get; set; }
        public bool MouseWheelUp { get; set; }
        public bool MouseWheelDown { get; set; }

        // Rear Hat (Throttle)
        public bool HatRearUp { get; set; }
        public bool HatRearDown { get; set; }
        public bool HatRearLeft { get; set; }
        public bool HatRearRight { get; set; }

        // Mouse Nub (Analog)
        public int MouseX { get; set; }
        public int MouseY { get; set; }

        public int ProductId { get; set; }
        public DateTime Timestamp { get; set; }
        public byte[]? RawData { get; set; } = null; // Initialize to null explicitly

        public X52State()
        {
            Timestamp = DateTime.UtcNow;
            RawData = null;
        }
    }
}
