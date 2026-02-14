# Ærakon x52 driver (v1.1.7) 💎

![Æ52 Icon](app_icon.png)

A modern, high-performance, and open-source driver for the **Logitech/Saitek X52 (Pro & Standard)**. Designed to replace the obsolete SST software with a focus on stability, precision, and hardware rescue.

## 🚀 Key Features

- **🛡️ Hardware Rescue (Silver Bullet Logic)**:
  - **Ghostbuster Filter**: Eliminates random inputs from the Mouse Nub (common hardware failure).
  - **Amputation Y**: Disables faulty nub axes to restore 100% functionality to **Hat 2**.
- **📈 Advanced Sensitivity Control**:
  - Independent **X and Y axis** multipliers (0.1x to 3.0x).
  - Real-time adjustment with visual feedback.
- **🎨 Premium Neon UI**:
  - Live hardware monitor for all 32 buttons and 7 axes.
  - Dynamic Mode Indicator synced with the physical X52 dial.
- **🔄 Smart Profiling**:
  - Auto-load profiles based on the running game executable (e.g., `DCS.exe`).
  - Native 64-bit .NET 9 performance.
- **📦 Ultra-lightweight**:
  - ~6MB installer compared to the hundreds of MBs of the original software.
  - Easy installation and uninstallation with custom "Æ52" branding.

## 🛠️ Requirements

1. **vJoy**: This driver sends data to a virtual joystick.
   - Download and install vJoy from [vJoy Official Site](http://vjoystick.sourceforge.net/).
   - Ensure **Device #1** is enabled in "Configure vJoy".
2. **.NET 9 Runtime**: Usually included with Windows 11 or installed automatically by the app.

## 💻 Installation

1. Download the latest `AerakonX52Driver_Setup_v1.1.7.exe` from the releases.
2. Run the installer.
3. Open the app and ensure the "vJoy Status" indicator is green.

## 🕹️ Why use this instead of the original?

The original Logitech software is over 20 years old and often conflicts with modern Windows security features (like Memory Integrity). This driver communicates directly with the HID raw data, ensuring zero lag, zero crashes, and solving the "drifting mouse" problem that plagues old X52 units.

## 🤝 Contributing

This is a **Platinum Release** of the Ærakon driver. Contributions are welcome for adding MFD control or more advanced macro features.

---
*Created with ❤️ by d2ndsky / Ærakon*
