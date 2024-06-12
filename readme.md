# HyperX Battery Monitor

HyperX Battery Monitor is a simple Windows system tray application that monitors the battery level of HyperX Cloud Flight wireless devices and displays their status. The application is built using C# with .NET Framework 4.8 and does not require installation (portable).

Made (in a few hours as a fun project) because I was sick and tired of the HyperX Ngenuity app in Windows just for checking the battery level status. Also in my machine this app was like 1.5GB which for me was unacceptable since all I wanted was a battery level monitor app.
This is a quick n dirty approach to completely replace that battery monitor utility from HyperX.


## Features

- Monitors battery level of HyperX Cloud Flight wireless devices.
- Displays device status and battery level in the system tray.
- Right-click context menu with "Exit" option.

## Prerequisites

- .NET Framework 4.8
- Windows 10/11
- Linux Mint (latest release, mono runtime, works with no issues)
- Will probably work on any Linux distribution having access to mono runtime

## Getting Started

### Running the Application

1. Navigate to the folder where you compiled the solution.
2. Double-click `HyperXBatteryMonitor.exe` to start the application.
3. The application will appear as an icon in the system tray.

### System Tray Icon

- **Hover (Active):** Displays the current status in the below formatting of your HyperX device.
    - `Headphones: Active`
    - `Battery: 100%`
- **Hover (Inactive):**
  - `Headphones: Inactive`
- **Right-Click Menu:**
    - `Exit`: Closes the application.

## Adding to Startup

To ensure the application starts automatically when you log in to Windows:

1. **Create a Shortcut:**
    - Right-click `HyperXBatteryMonitor.exe` and select `Create shortcut`.

2. **Move the Shortcut to the Startup Folder:**
    - Press `Win + R`, type `shell:startup`, and press Enter.
    - Move the created shortcut to this folder.

## Building from Source

### Prerequisites

- Visual Studio 2019 or later
- JetBrains Rider

### Steps

1. Clone the repository:
   ```bash
   git clone https://github.com/simonkaps/HyperXBatteryMonitor.git
