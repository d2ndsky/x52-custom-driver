using HidLibrary;

namespace X52.CustomDriver.Core.Hardware
{
    public interface IHidService
    {
        bool Initialize();
        void SendFeatureReport(byte[] data);
        void SetLed(int ledId, int state);
        string GetDevicePath();
    }
}
