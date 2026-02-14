using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;

namespace X52.CustomDriver.Core.Services
{
    public class KeyboardService
    {
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
        
        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT { public uint uMsg; public ushort wParamL; public ushort wParamH; }

        const uint INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        // Map of friendly names to Virtual Key codes
        private static readonly Dictionary<string, ushort> KeyMap = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
        {
            // Modifiers
            { "LSHIFT", 0xA0 }, { "RSHIFT", 0xA1 }, { "LCTRL", 0xA2 }, { "RCTRL", 0xA3 }, { "LALT", 0xA4 }, { "RALT", 0xA5 },
            // Letters A-Z
            { "A", 0x41 }, { "B", 0x42 }, { "C", 0x43 }, { "D", 0x44 }, { "E", 0x45 }, { "F", 0x46 }, { "G", 0x47 }, { "H", 0x48 }, 
            { "I", 0x49 }, { "J", 0x4A }, { "K", 0x4B }, { "L", 0x4C }, { "M", 0x4D }, { "N", 0x4E }, { "O", 0x4F }, { "P", 0x50 }, 
            { "Q", 0x51 }, { "R", 0x52 }, { "S", 0x53 }, { "T", 0x54 }, { "U", 0x55 }, { "V", 0x56 }, { "W", 0x57 }, { "X", 0x58 }, 
            { "Y", 0x59 }, { "Z", 0x5A },
            // Digits 0-9
            { "0", 0x30 }, { "1", 0x31 }, { "2", 0x32 }, { "3", 0x33 }, { "4", 0x34 }, { "5", 0x35 }, { "6", 0x36 }, { "7", 0x37 }, { "8", 0x38 }, { "9", 0x39 },
            // Function Keys F1-F12
            { "F1", 0x70 }, { "F2", 0x71 }, { "F3", 0x72 }, { "F4", 0x73 }, { "F5", 0x74 }, { "F6", 0x75 }, { "F7", 0x76 }, { "F8", 0x77 }, { "F9", 0x78 }, { "F10", 0x79 }, { "F11", 0x7A }, { "F12", 0x7B },
            // Arrows
            { "UP", 0x26 }, { "DOWN", 0x28 }, { "LEFT", 0x25 }, { "RIGHT", 0x27 },
            // Common
            { "SPACE", 0x20 }, { "ENTER", 0x0D }, { "ESCAPE", 0x1B }, { "TAB", 0x09 }, { "BACKSPACE", 0x08 }, { "LWIN", 0x5B }
        };

        public void SendKeys(List<string> keys)
        {
            var vkCodes = keys.Select(k => KeyMap.ContainsKey(k) ? KeyMap[k] : (ushort)0).Where(v => v != 0).ToList();
            if (vkCodes.Count == 0) return;

            var inputs = new List<INPUT>();

            // Press all modifiers/keys in order
            foreach (var vk in vkCodes)
            {
                inputs.Add(new INPUT { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wVk = vk } } });
            }

            // Release all in reverse order
            foreach (var vk in vkCodes.AsEnumerable().Reverse())
            {
                inputs.Add(new INPUT { type = INPUT_KEYBOARD, u = new InputUnion { ki = new KEYBDINPUT { wVk = vk, dwFlags = KEYEVENTF_KEYUP } } });
            }

            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
