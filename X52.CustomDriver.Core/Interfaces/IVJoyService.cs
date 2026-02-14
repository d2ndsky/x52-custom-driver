using System;

namespace X52.CustomDriver.Core.Interfaces
{
    public interface IVJoyService
    {
        bool IsAvailable { get; }
        uint DeviceId { get; }
        string DeviceName { get; }
        
        bool Initialize(uint deviceId);
        void Shutdown();
        
        void SetAxisX(int value);
        void SetAxisY(int value);
        void SetAxisZ(int value);
        void SetRx(int value);
        void SetRy(int value);
        void SetRz(int value);
        void SetButtons(uint buttons);
        void SetButton(int buttonId, bool pressed);
        void SetSlider(int value, int index = 0);
        void SetDial(int value);
    }
}
