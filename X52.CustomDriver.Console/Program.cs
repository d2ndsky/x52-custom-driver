using System;
using System.Threading.Tasks;
using X52.CustomDriver.Core.Interfaces;
using X52.CustomDriver.Core.Services;
using X52.CustomDriver.Core.Models;

namespace X52.CustomDriver.ConsoleHost
{
    class Program
    {
        private static IVJoyService _vJoyService = new VJoyService();
        private static IHidService _hidService = new X52HidService();
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("   X52 Custom Driver - Core Services");
            Console.WriteLine("========================================");

            if (_vJoyService.Initialize(1))
            {
                Console.WriteLine("[SUCCESS] vJoy Device 1 Acquired.");
            }
            else
            {
                Console.WriteLine("[WARNING] vJoy not available. Running in Mapping Mode.");
            }

            _hidService.OnStateChanged += HidService_OnStateChanged;
            _hidService.OnError += (s, e) => Console.WriteLine($"\n[HID ERROR] {e}");
            
            _hidService.Initialize();

            if (_hidService.IsConnected)
            {
                // We'll get the PID from the service to show it here
                Console.WriteLine($"[SUCCESS] X52 Connected.");
                _hidService.StartListening();
                
                Console.Clear();
                Console.WriteLine("========================================");
                Console.WriteLine("   X52 Custom Driver - Diagnostics");
                Console.WriteLine("========================================");
                Console.WriteLine("\n[WAITING] Move any axis or button...");
                await Task.Delay(-1); 
            }
            else
            {
                Console.WriteLine("[ERROR] X52 not found.");
            }
            
            _hidService.StopListening();
            _vJoyService.Shutdown();
        }

        private static int[] _bitPersistence = new int[16 * 8];
        private static bool[] _stableBits = new bool[16 * 8];

        private static void HidService_OnStateChanged(object? sender, X52State state)
        {
            if (_vJoyService.IsAvailable)
            {
                // --- AXES MAPPING ---
                int vX = state.X * 16;
                int vY = state.Y * 16;
                int vZ = state.Z * 32;

                // Throttle Calibration: 
                // User reports ~2mm deadzone at the start.
                // Aggressive mapping: Physical [245 -> 10] maps to [255 -> 0]
                int calibratedThrottle = ApplyCalibration(state.Throttle, 255, 0, 255, 0);
                int vT = (255 - calibratedThrottle) * 128; // Inverted for 0-100% vJoy
                
                // Rotaries with Deadzone (Center is 127)
                int rawR1 = ApplySoftwareDeadzone(state.Rotary1, 127, 12);
                int rawR2 = ApplySoftwareDeadzone(state.Rotary2, 127, 4);
                
                int vR1 = rawR1 * 128;
                int vR2 = rawR2 * 128;
                int vS = state.Slider * 128;

                // Mouse Nub Mapping
                int vMX = 16384 + (ApplySoftwareDeadzone(state.MouseX, 0, 5) * 128);
                int vMY = 16384 + (ApplySoftwareDeadzone(state.MouseY, 0, 5) * 128);
                
                _vJoyService.SetAxisX(vX);
                _vJoyService.SetAxisY(vY);
                _vJoyService.SetAxisZ(vZ);
                _vJoyService.SetRx(vT);
                _vJoyService.SetRy(vR1);
                _vJoyService.SetRz(vR2);
                _vJoyService.SetSlider(vS);
                _vJoyService.SetSlider(vMX, 1); // Slider 2
                _vJoyService.SetDial(vMY);       // Dial

                // --- STABLE HUNTER ---
                UpdateStableHunter(state);

                // --- VIRTUAL BUTTON REDIRECTOR ---
                UpdateVirtualButtons(state);

                // --- HUD OUTPUT ---
                ShowDiagnosticInfo(state, vX, vY, vZ, rawR1, rawR2);
            }
        }

        private static int ApplyCalibration(int value, int inMin, int inMax, int outMin, int outMax)
        {
            // Debugging: Log if values change meaningfully
            int original = value;
            
            // Clamp input to the defined "active" range
            if (inMin > inMax)
            {
                if (value > inMin) value = inMin;
                if (value < inMax) value = inMax;
            }
            else
            {
                if (value < inMin) value = inMin;
                if (value > inMax) value = inMax;
            }

            // Map the range [inMin, inMax] to [outMin, outMax]
            int result = (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
            
            // Log only for throttle range (255 down to 0)
            if (original != result && original > 200) 
            {
                 // Small internal log or HUD update
            }

            return result;
        }

        private static void UpdateStableHunter(X52State state)
        {
            if (state.RawData == null) return;
            for (int b = 8; b < Math.Min(state.RawData.Length, 16); b++)
            {
                for (int bit = 0; bit < 8; bit++)
                {
                    int bitIdx = b * 8 + bit;
                    bool currentValue = (state.RawData[b] & (1 << bit)) != 0;

                    if (currentValue != _stableBits[bitIdx])
                    {
                        _bitPersistence[bitIdx]++;
                        if (_bitPersistence[bitIdx] > 0)
                        {
                            if (!((b == 10 && bit == 7) || (b == 13 && bit == 3) || (b == 13 && bit == 7)))
                            {
                                _stableBits[bitIdx] = currentValue;
                                _bitPersistence[bitIdx] = 0;
                                Console.WriteLine($"\n[STABLE HUNTER] B{b}, Bit {bit} -> {(_stableBits[bitIdx] ? "PRESSED" : "RELEASED")}");
                            }
                            else
                            {
                                _stableBits[bitIdx] = currentValue;
                                _bitPersistence[bitIdx] = 0;
                            }
                        }
                    }
                    else
                    {
                        _bitPersistence[bitIdx] = 0;
                    }
                }
            }
        }

        private static void UpdateVirtualButtons(X52State state)
        {
            if (state.RawData == null) return;
            int vBtn = 1 + (state.CurrentMode - 1) * 32;
            // Limit to B11 to avoid mapping Raw Hat 2 (B12) and Nub Noise (B13) to buttons 33-48
            for (int b = 8; b < Math.Min(state.RawData.Length, 12); b++)
            {
                for (int bit = 0; bit < 8; bit++)
                {
                    // GHOSTBUSTER: Ignore internal Mode bits from being mapped as virtual buttons
                    // B10 Bit 7 = Mode 1 (Standard)
                    // B11 Bits 0 & 1 = Mode 2 & 3 (Standard)
                    if ((b == 10 && bit == 7) || (b == 11 && (bit == 0 || bit == 1)))
                    {
                        vBtn++;
                        continue;
                    }

                    if (vBtn <= 128)
                        _vJoyService.SetButton(vBtn++, (state.RawData[b] & (1 << bit)) != 0);
                }
            }

            // SILVER BULLET PHASE 2: Explicit Clean Mapping for Hat 2
            // We override buttons 29-32 (which might have been 0'd by B11 upper nibble)
            // baseBtn is 1. +28 -> 29.
            int baseId = 1 + (state.CurrentMode - 1) * 32;
            _vJoyService.SetButton(baseId + 28, state.Hat2Up);    // Button 29
            _vJoyService.SetButton(baseId + 29, state.Hat2Down);  // Button 30
            _vJoyService.SetButton(baseId + 30, state.Hat2Left);  // Button 31
            _vJoyService.SetButton(baseId + 31, state.Hat2Right); // Button 32
        }

        private static void ShowDiagnosticInfo(X52State state, int vX, int vY, int vZ, int rawR1, int rawR2)
        {
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("==================================================================");
            Console.WriteLine("   X52 Custom Driver - Live Bit Matrix & Diagnostics");
            Console.WriteLine("==================================================================");
            Console.WriteLine($"[ESC/Q] to Exit | Project: X52 sovereign driver v1.1.7 (SENSITIVITY)".PadRight(Console.WindowWidth));
            
            string hex = state.RawData != null ? string.Join(" ", state.RawData.Take(15).Select(be => be.ToString("X2"))) : "NODATA";
            int len = state.RawData?.Length ?? 0;
            Console.SetCursorPosition(0, 4);
            Console.WriteLine($"RAW({len:00}): {hex} | [M{state.CurrentMode}] [ID:{state.ProductId:X4}]".PadRight(Console.WindowWidth));
            
            string axes = $"AXES: X:{vX-16384,6} Y:{vY-16384,6} Z:{vZ-16384,6} T:{state.Throttle,3} S:{state.Slider,3} R1:{rawR1,3} R2:{rawR2,3}";
            Console.WriteLine(axes.PadRight(Console.WindowWidth));
            
            string h1 = $"HAT1: {(state.Hat1Up?"U":" ")}{(state.Hat1Down?"D":" ")}{(state.Hat1Left?"L":" ")}{(state.Hat1Right?"R":" ")}";
            string h2 = $"HAT2: {(state.Hat2Up?"U":" ")}{(state.Hat2Down?"D":" ")}{(state.Hat2Left?"L":" ")}{(state.Hat2Right?"R":" ")}";
            string mfd = $"MFD:[{(state.MfdFunction?"F":" ")}][{(state.MfdStartStop?"S":" ")}][{(state.MfdReset?"R":" ")}] C:{(state.ClutchButton?"X":" ")} D:{(state.ButtonD?"X":" ")} E: {(state.ButtonE?"X":" ")}";
            Console.WriteLine($"{h1.PadRight(15)} | {h2.PadRight(15)} | {mfd.PadRight(40)} | P: {(state.Pinkie?"ON ":"OFF")}".PadRight(Console.WindowWidth));
            
            string mouse = $"MOUSE: [{(state.MouseLeftClick?"L":" ")}][{(state.MouseNubClick?"N":" ")}][{(state.MouseWheelClick?"W":" ")}] X:{state.MouseX,4} Y:{state.MouseY,4}";
            string wheel = $"Wheel: [{(state.MouseWheelUp?"U":" ")}][{(state.MouseWheelDown?"D":" ")}]";
            string hr = $"REAR: {(state.HatRearUp?"U":" ")}{(state.HatRearDown?"D":" ")}{(state.HatRearLeft?"L":" ")}{(state.HatRearRight?"R":" ")}";
            Console.WriteLine($"{mouse.PadRight(35)} | {wheel.PadRight(15)} | {hr.PadRight(15)}".PadRight(Console.WindowWidth));
            
            Console.WriteLine("------------------------------------------------------------------".PadRight(Console.WindowWidth));
            
            if (state.RawData != null)
            {
                for (int b = 8; b < Math.Min(state.RawData.Length, 15); b++)
                {
                    string bin = Convert.ToString(state.RawData[b], 2).PadLeft(8, '0');
                    string labels = b == 8 ? "<- [7:? 6:D 5:Pinkie 4:C | 3:B 2:A 1:Fire 0:TrigS2]" :
                                   b == 9 ? "<- [7:H1-U 6:TrigS1 5:T6 4:T5 | 3:T4 2:T3 1:T2 0:T1]" :
                                   b == 10? $"<- [7:M1  6:RL 5:RD 4:RU | 3:RR 2:HL 1:HD 0:HR]" : 
                                   b == 11? $"<- MFD/MouseBtns [Binary]" :
                                   b == 12? $"<- HAT 2 (DISCRETE VALS) (Dec: {state.RawData[b]})" :
                                   b == 13? $"<- NUB Y (IGNORED)       (Dec: {state.RawData[b]})" :
                                   b == 14? $"<- (Dec: {state.RawData[b]})" : "";

                    Console.WriteLine($"B{b:00} [{state.RawData[b]:X2}]: {bin[0]} {bin[1]} {bin[2]} {bin[3]} | {bin[4]} {bin[5]} {bin[6]} {bin[7]}  {labels}".PadRight(Console.WindowWidth));
                }
            }
            Console.WriteLine("------------------------------------------------------------------".PadRight(Console.WindowWidth));
            Console.WriteLine("Logs (Recent events):".PadRight(Console.WindowWidth));
        }
        
        static int ApplySoftwareDeadzone(int value, int center, int deadzone)
        {
            if (Math.Abs(value - center) < deadzone) return center;
            return value;
        }

        static int byte_index(int i) => i; // Helper placeholder
    }
}
