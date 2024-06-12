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
        private static readonly ushort[] VENDOR_IDS = { 0x0951, 0x03F0 };
        private static readonly ushort[] PRODUCT_IDS = { 0x1718, 0x1723, 0x1725, 0x018B, 0x0D93 };
        private static readonly byte[] BATTERY_PACKET = { 0x21, 0xFF, 0x05, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        private NotifyIcon notifyIcon;
        private Timer timer;

        public SystemTrayApp() {
            notifyIcon = new NotifyIcon {
                Icon = GetEmbeddedIcon("headset-32.ico"),
                Visible = true,
                ContextMenuStrip = new ContextMenuStrip()
            };
            notifyIcon.ContextMenuStrip.Items.Add("Exit", null, Exit);
            timer = new Timer { Interval = 10000 }; // Check every minute
            timer.Tick += Timer_Tick;
            timer.Start();
            UpdateBatteryStatus();
        }

        private void Timer_Tick(object sender, EventArgs e) { UpdateBatteryStatus(); }

        private void UpdateBatteryStatus() {
            try {
                PollDevice();
            }
            catch (Exception) {
                notifyIcon.Text = "Headphones: Inactive";
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
                            var batteryLevel = TryGetBatteryLevel(hidStream);
                            if (batteryLevel == 255) continue;
                            var batteryStatus = $"Headphones: Active\nBattery: {batteryLevel}%";
                            notifyIcon.Text = batteryStatus;
                            hidStream.Close();
                            return;
                        } catch (Exception) {
                            notifyIcon.Text = "Headphones: Inactive";
                            hidStream.Close();
                        }
                    }
                }
            }
            notifyIcon.Text = "Headphones: Inactive";
        }

        private static int TryGetBatteryLevel(HidStream hidStream) {
            TryWritePacketToStream(hidStream, BATTERY_PACKET);
            byte[] buffer = TryReadPacketFromStream(hidStream);
            if (buffer == null) return 255;
            var magicValue = buffer[4] != 0 ? buffer[4] : buffer[3];
            var chargeState = buffer[3];
            return CalculatePercentage(chargeState, magicValue);
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

        private static int CalculatePercentage(int chargeState, int magicValue) {
            if (chargeState == 0x10)
            {
                return magicValue <= 11 ? 200 : 199;
            }

            if (chargeState == 0xf)
            {
                if (magicValue >= 130) return 100;
                if (magicValue >= 120) return 95;
                if (magicValue >= 100) return 90;
                if (magicValue >= 70) return 85;
                if (magicValue >= 50) return 80;
                if (magicValue >= 20) return 75;
                if (magicValue > 0) return 70;
            }

            if (chargeState == 0xe)
            {
                if (magicValue > 240) return 65;
                if (magicValue >= 220) return 60;
                if (magicValue >= 208) return 55;
                if (magicValue >= 200) return 50;
                if (magicValue >= 190) return 45;
                if (magicValue >= 180) return 40;
                if (magicValue >= 169) return 35;
                if (magicValue >= 159) return 30;
                if (magicValue >= 148) return 25;
                if (magicValue >= 119) return 20;
                if (magicValue >= 90) return 15;
                if (magicValue < 90) return 10;
                return 66;
            }

            // means error value of 255
            return 255;
        }


        private void Exit(object sender, EventArgs e) {
            notifyIcon.Visible = false;
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
