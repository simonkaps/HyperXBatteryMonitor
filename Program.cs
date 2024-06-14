using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using HidSharp;

namespace HyperXBatteryMonitor {
    static class Program {
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SystemTrayApp());
        }
        
    }

    public class SystemTrayApp : ApplicationContext {
        private static readonly ushort[] VENDOR_IDS = { 0x0951 };
        private static readonly ushort[] PRODUCT_IDS = { 0x1723, 0x16c4 };
        private static readonly byte[] BATTERY_PACKET = { 0x21, 0xFF, 0x05, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private static readonly string NOTIFY_ICON_TEXT = "Headphones: Inactive";
        private static int _batteryLevel = 255;
        private static bool _isCharging;
        private readonly NotifyIcon _notifyIcon;
        private readonly Timer _timer;

        public SystemTrayApp() {
            _notifyIcon = new NotifyIcon {
                Icon = GetEmbeddedIcon("headset-32.ico"),
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip()
            };
            _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, Exit);
            _timer = new Timer { Interval = 10000 }; // Check / Poll every 10 seconds
            _timer.Tick += Timer_Tick;
            _timer.Start();
            UpdateBatteryStatus();
        }

        private static string GetChargingStatus() { return _isCharging ? "Charging" : "Active"; }
        private static string GetBatteryLevelStatus() {
            return _batteryLevel == 255 ? "N/A" : $"Headphones: {GetChargingStatus()}\nBattery: {_batteryLevel}%";
        }

        private void Timer_Tick(object sender, EventArgs e) { UpdateBatteryStatus(); }

        private void UpdateBatteryStatus() {
            try {
                PollDevice();
            }
            catch (Exception) {
                UpdateNotificationText(NOTIFY_ICON_TEXT);
            }
        }

        private void PollDevice() {
            var devices = DeviceList.Local.GetHidDevices().ToList();
            var vendorIds = VENDOR_IDS.Select(v => (int)v);
            var productIds = PRODUCT_IDS.Select(p => (int)p);
            foreach (var device in devices) {
                if (vendorIds.Contains(device.VendorID) && productIds.Contains(device.ProductID)) {
                    if (device.TryOpen(out HidStream hidStream)) {
                        try {
                            TryGetBatteryLevel(hidStream);
                            if (_batteryLevel == 255) continue;
                            UpdateNotificationText(GetBatteryLevelStatus());
                            hidStream.Close();
                            return;
                        } catch (Exception) {
                            UpdateNotificationText(NOTIFY_ICON_TEXT);
                            hidStream.Close();
                        }
                    }
                }
            }
            UpdateNotificationText(NOTIFY_ICON_TEXT);
        }

        private static void TryGetBatteryLevel(HidStream hidStream) {
            TryWritePacketToStream(hidStream, BATTERY_PACKET);
            byte[] buffer = TryReadPacketFromStream(hidStream);
            if (buffer == null) {
                _batteryLevel = 255;
                return;
            }
            Console.WriteLine($@"[packet response contents] {BitConverter.ToString(buffer)}");
            _isCharging = buffer[3] == 0x10 || buffer[3] == 0x11;
            _batteryLevel = CalculatePercentage(buffer[3], buffer[4]);
        }

        private static void TryWritePacketToStream(HidStream hidStream, byte[] packet) {
            hidStream.Write(packet, 0, packet.Length);
        }

        private static byte[] TryReadPacketFromStream(HidStream hidStream, int length = 8) {
            byte[] buffer = new byte[length];
            int bytesRead = hidStream.ReadAsync(buffer, 0, buffer.Length).GetAwaiter().GetResult();
            if (bytesRead == 0 || buffer.All(b => b == 0)) return null;
            return buffer;
        }

        private static byte CalculatePercentage(byte chargeState, byte value) {
            if (chargeState == 0x0e) {
                if (value <= 89) return 10;
                if (value <= 119) return 15;
                if (value <= 148) return 20;
                if (value <= 159) return 25;
                if (value <= 169) return 30;
                if (value <= 179) return 35;
                if (value <= 189) return 40;
                if (value <= 199) return 45;
                if (value <= 209) return 50;
                if (value <= 219) return 55;
                if (value <= 239) return 60;
                return 65;
            }
            if (chargeState == 0x0f) {
                if (value <= 19) return 70;
                if (value <= 49) return 75;
                if (value <= 69) return 80;
                if (value <= 99) return 85;
                if (value <= 119) return 90;
                if (value <= 129) return 95;
                return 100;
            }
            return 100;
        }
        private void UpdateNotificationText(string text) { if (_notifyIcon.Text != text) _notifyIcon.Text = text; }

        private void Exit(object sender, EventArgs e) {
            _notifyIcon.Visible = false;
            Application.Exit();
        }
        
        private Icon GetEmbeddedIcon(string resourceName) {
            string fullResourceName = $"{typeof(Program).Namespace}.{resourceName}";
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullResourceName)) {
                if (stream != null) return new Icon(stream);
            }
            return null;
        }
    }
}
