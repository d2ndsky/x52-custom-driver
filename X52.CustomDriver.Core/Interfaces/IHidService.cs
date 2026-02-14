using System;
using X52.CustomDriver.Core.Models;

namespace X52.CustomDriver.Core.Interfaces
{
    public interface IHidService
    {
        bool IsConnected { get; }
        void Initialize();
        void StartListening();
        void StopListening();
        
        // LED and MFD Control
        void SetLed(byte ledId, byte state);
        void SetBrightness(byte brightness);
        
        // Calibration
        void ResetCalibration();
        void CalibrateCenter();
        
        event EventHandler<X52State> OnStateChanged;
        event EventHandler<string> OnError;
    }
}
