using HidLibrary;
using System.Linq;
using System;

namespace X52.CustomDriver.Core.Hardware
{
    public class X52HidService : IHidService
    {
        private HidDevice _device;
        // Known X52 IDs
        private const int VID = 0x06A3;
        private const int PID_X52 = 0x075C; // Old X52
        private const int PID_X52PRO = 0x0762; // X52 Pro

        public bool Initialize()
        {
            // Try enabling different IDs
            _device = HidDevices.Enumerate(VID, PID_X52PRO).FirstOrDefault() 
                      ?? HidDevices.Enumerate(VID, PID_X52).FirstOrDefault();

            if (_device == null)
            {
                Console.WriteLine("X52 Device not found!");
                return false;
            }

            _device.OpenDevice();
            Console.WriteLine($"Connected to: {_device.Description} ({_device.DevicePath})");
            
            // Hook up event handlers if needed
            // _device.ReadReport(OnReport); 

            return true;
        }

        public string GetDevicePath()
        {
            return _device?.DevicePath ?? "Not Connected";
        }

        public void SendFeatureReport(byte[] data)
        {
            if (_device == null || !_device.IsOpen) return;
            
            try 
            {
                int reportLength = _device.Capabilities.FeatureReportByteLength;
                
                // Fallback for X52 if report length is invalid (0 or -1)
                if (reportLength <= 0)
                {
                     // Standard Saitek/Logitech feature report size usually 8 bytes.
                     reportLength = 8; 
                }

                // Create a buffer of the correct size
                byte[] paddedData = new byte[reportLength];
                
                // Strategy 2: Output Report (WriteReport)
                // Some older X52s use Output Reports for LEDs instead of Feature Reports.
                // Output Reports usually have ReportID 0 or specific.
                
                // Let's try sending as Output Report
                bool success = _device.WriteReport(new HidReport(reportLength, new HidDeviceData(paddedData, HidDeviceData.ReadStatus.Success)));
                if (success) Console.WriteLine("Command Sent via WriteReport.");
                else 
                {
                    // Fallback to Feature Data
                    _device.WriteFeatureData(paddedData);
                    Console.WriteLine("Command Sent via WriteFeatureData.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] SendFeatureReport Failed: {ex.Message}");
            }
        }

        // Basic LED Logic (Reverse Engineered Reference)
        // X52 Pro LED Command usually: Index 0=Command, 1=Val
        public void SetLed(int ledId, int state)
        {
            // This is a placeholder for the actual byte sequence.
            // We will need to test specific bytes.
            // Common X52 Pro LED Page: 0xB8
            
            // Example structure (needs verification):
            // Byte 0: Report ID (0x00 or dedicated feature ID)
            // Byte 1: Command (0xB8 for LEDs)
            // Byte 2: LED ID
            // Byte 3: State (0=Off, 1=Green, 2=Amber, 3=Red)
            
            byte[] command = new byte[] { 0xB8, (byte)ledId, (byte)state }; 
            // Note: WriteFeatureData usually takes the ReportID as separate or first byte.
            // We'll trust HidLibrary to handle the raw buffer if we pass it correctly.
            
            SendFeatureReport(command);
        }
    }
}
