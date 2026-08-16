using System;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using LibreHardwareMonitor.Hardware;

namespace ArduinoTempMonitor
{
    public class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private SerialPort serialPort;
        private System.Windows.Forms.Timer pollTimer;

        // Hardware Monitor
        private Computer computer;
        private UpdateVisitor updateVisitor = new UpdateVisitor(); // Add this line

        // UI Controls
        private ComboBox cmbComPorts;
        private Button btnRefresh;
        private ComboBox cmbPollingRate;
        private Button btnConnect;
        private Button btnDisconnect;

        private Label lblCpu;
        private Label lblCpuTemp;
        private Label lblGpu;
        private Label lblGpuTemp;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        // Tray Icons
        private Icon iconGreen;
        private Icon iconRed;

        public Form1()
        {
            InitializeHardwareMonitor();
            InitializeUI();
            LoadComPorts();
            SetupTrayIcon();

            // Show initial temperatures immediately
            lblCpuTemp.Text = $"{GetCpuTemperature():F0} °C";
            lblGpuTemp.Text = $"{GetGpuHotSpotTemperature():F0} °C";

            // Start the timer immediately so the UI updates live
            UpdateTimerInterval();
            pollTimer.Start();
        }

        private void InitializeHardwareMonitor()
        {
            computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = false,
                IsStorageEnabled = false,
                IsMotherboardEnabled = false,
                IsNetworkEnabled = false,
                IsControllerEnabled = false
            };
            computer.Open();
            computer.Accept(updateVisitor);
        }

        private void InitializeUI()
        {
            this.Text = "Arduino-CPU-And-GPU-Temp-Monitor";

            // FIX: Use ClientSize so borders don't cut off the buttons
            this.ClientSize = new Size(420, 180);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimizeBox = true;

            int y = 20;
            int dropdownWidth = 200;
            int buttonWidth = 80;
            int spacing = 10;
            int x = 15;

            // Row 1: COM Port & Refresh
            cmbComPorts = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = dropdownWidth, Location = new Point(x, y) };
            btnRefresh = new Button { Text = "Refresh", Width = buttonWidth, Location = new Point(x + dropdownWidth + spacing, y) };
            btnRefresh.Click += (s, e) => LoadComPorts();

            // Row 2: Polling Rate, Connect, Disconnect
            y += 35;
            cmbPollingRate = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = dropdownWidth, Location = new Point(x, y) };
            cmbPollingRate.Items.AddRange(new object[] { "1.0 s", "0.75 s", "0.5 s", "0.25 s", "0.1 s" });
            cmbPollingRate.SelectedIndex = 2;

            btnConnect = new Button { Text = "Connect", Width = buttonWidth, Location = new Point(x + dropdownWidth + spacing, y) };
            btnDisconnect = new Button { Text = "Disconnect", Width = buttonWidth + 10, Location = new Point(x + dropdownWidth + spacing + buttonWidth + spacing, y), Enabled = false };

            btnConnect.Click += BtnConnect_Click;
            btnDisconnect.Click += BtnDisconnect_Click;

            // Row 3: CPU Temp
            y += 45;
            lblCpu = new Label { Text = "CPU Temp:", Location = new Point(x, y), AutoSize = true, Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold) };
            lblCpuTemp = new Label { Text = "0 °C", Location = new Point(x + 100, y), AutoSize = true, Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold) };

            // Row 4: GPU Temp
            y += 35;
            lblGpu = new Label { Text = "GPU Temp:", Location = new Point(x, y), AutoSize = true, Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold) };
            lblGpuTemp = new Label { Text = "0 °C", Location = new Point(x + 100, y), AutoSize = true, Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold) };

            this.Controls.Add(cmbComPorts);
            this.Controls.Add(btnRefresh);
            this.Controls.Add(cmbPollingRate);
            this.Controls.Add(btnConnect);
            this.Controls.Add(btnDisconnect);
            this.Controls.Add(lblCpu);
            this.Controls.Add(lblCpuTemp);
            this.Controls.Add(lblGpu);
            this.Controls.Add(lblGpuTemp);

            pollTimer = new System.Windows.Forms.Timer();
            pollTimer.Tick += PollTimer_Tick;
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MINIMIZE = 0xF020;
            if (m.Msg == WM_SYSCOMMAND && m.WParam.ToInt32() == SC_MINIMIZE)
            {
                this.Hide();
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        private void LoadComPorts()
        {
            string selectedPort = cmbComPorts.SelectedItem?.ToString();
            cmbComPorts.Items.Clear();
            cmbComPorts.Items.AddRange(SerialPort.GetPortNames());
            if (cmbComPorts.Items.Count == 0) cmbComPorts.Items.Add("No COM ports found");
            if (selectedPort != null && cmbComPorts.Items.Contains(selectedPort))
                cmbComPorts.SelectedItem = selectedPort;
            else
                cmbComPorts.SelectedIndex = 0;
        }

        private void SetupTrayIcon()
        {
            iconGreen = CreateSolidIcon(Color.Green);
            iconRed = CreateSolidIcon(Color.Red);
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show", null, (s, e) => { ShowWindow(); });
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Close", null, (s, e) => { Application.Exit(); });
            trayIcon = new NotifyIcon { Icon = iconRed, ContextMenuStrip = trayMenu, Visible = true, Text = "Temp Monitor - Disconnected" };
            trayIcon.DoubleClick += (s, e) => { ShowWindow(); };
        }

        private void ShowWindow()
        {
            if (this.InvokeRequired) { this.BeginInvoke((MethodInvoker)delegate { ShowWindow(); }); return; }
            this.Show();
            SetForegroundWindow(this.Handle);
            this.Activate();
        }

        private Icon CreateSolidIcon(Color color)
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp)) { g.Clear(color); }
            return Icon.FromHandle(bmp.GetHicon());
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (cmbComPorts.SelectedItem == null || cmbComPorts.SelectedItem.ToString().StartsWith("No")) return;
            try
            {
                serialPort = new SerialPort(cmbComPorts.SelectedItem.ToString(), 19200);
                serialPort.Open();
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
                cmbComPorts.Enabled = false;
                btnRefresh.Enabled = false;
                trayIcon.Icon = iconGreen;
                trayIcon.Text = "Temp Monitor - Connected";
                UpdateTimerInterval();
                pollTimer.Start();
            }
            catch
            {
                MessageBox.Show("Could not open COM Port.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SilentDisconnect();
            }
        }

        private void BtnDisconnect_Click(object sender, EventArgs e) { SilentDisconnect(); }

        private void SilentDisconnect()
        {
            pollTimer.Stop();
            if (serialPort != null && serialPort.IsOpen) { try { serialPort.Close(); } catch { } }
            serialPort = null;
            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            cmbComPorts.Enabled = true;
            btnRefresh.Enabled = true;
            lblCpuTemp.Text = "0 °C";
            lblGpuTemp.Text = "0 °C";
            trayIcon.Icon = iconRed;
            trayIcon.Text = "Temp Monitor - Disconnected";
        }

        private void UpdateTimerInterval()
        {
            string selected = cmbPollingRate.SelectedItem.ToString();
            int ms = 500;
            if (selected.Contains("1.0")) ms = 1000;
            else if (selected.Contains("0.75")) ms = 750;
            else if (selected.Contains("0.5")) ms = 500;
            else if (selected.Contains("0.25")) ms = 250;
            else if (selected.Contains("0.1")) ms = 100;
            pollTimer.Interval = ms;
        }

        private async void PollTimer_Tick(object sender, EventArgs e)
        {
            UpdateTimerInterval();

            float cpuTemp = 0;
            float gpuTemp = 0;

            // Offload hardware reading to a background thread to prevent UI freezing
            await System.Threading.Tasks.Task.Run(() =>
            {
                computer.Accept(updateVisitor);
                cpuTemp = GetCpuTemperature();
                gpuTemp = GetGpuHotSpotTemperature();
            });

            // Now that we have the numbers, quickly update the UI on the main thread
            try
            {
                lblCpuTemp.Text = $"{cpuTemp:F0} °C";
                lblGpuTemp.Text = $"{gpuTemp:F0} °C";
            }
            catch { }

            // Send combined string: "CPU,GPU\n" (Only if connected)
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.WriteLine($"{cpuTemp:F0},{gpuTemp:F0}");
                }
            }
            catch
            {
                SilentDisconnect();
            }
        }

        private float GetCpuTemperature()
        {
            try
            {
                var cpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
                if (cpu != null)
                {
                    var temps = cpu.Sensors.Where(s => s.SensorType == SensorType.Temperature).ToList();
                    if (temps.Any())
                    {
                        // Try to find the overall package/temp first
                        var packageTemp = temps.FirstOrDefault(s => s.Name.Contains("Package") || s.Name.Contains("Tdie") || s.Name.Contains("Core (Tctl/Tdie)"));
                        if (packageTemp != null && packageTemp.Value.HasValue) return packageTemp.Value.Value;

                        // Fallback: Just return the highest reported core temperature
                        return temps.Max(s => s.Value ?? 0f);
                    }
                }
            }
            catch { }
            return 0f;
        }

        private float GetGpuHotSpotTemperature()
        {
            try
            {
                var gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuAmd || h.HardwareType == HardwareType.GpuNvidia);
                if (gpu != null)
                {
                    var temps = gpu.Sensors.Where(s => s.SensorType == SensorType.Temperature).ToList();
                    if (temps.Any())
                    {
                        // Look specifically for HotSpot / Junction
                        var hotSpotTemp = temps.FirstOrDefault(s => s.Name.Contains("Hot Spot") || s.Name.Contains("Junction") || s.Name.Contains("GPU Hotspot"));
                        if (hotSpotTemp != null && hotSpotTemp.Value.HasValue) return hotSpotTemp.Value.Value;

                        // Fallback: If the specific name isn't found (e.g. brand new RDNA4 GPU),
                        // the HotSpot is ALWAYS the highest temperature reported on the card.
                        return temps.Max(s => s.Value ?? 0f);
                    }
                }
            }
            catch { }
            return 0f;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; this.Hide(); return; }

            pollTimer?.Stop();
            if (serialPort != null && serialPort.IsOpen) serialPort.Close();
            computer?.Close();

            trayIcon?.Dispose();
            iconGreen?.Dispose();
            iconRed?.Dispose();
            base.OnFormClosing(e);
        }
    }

    // Required by LibreHardwareMonitor to update sensor values
    internal class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) { computer.Traverse(this); }
        public void VisitHardware(IHardware hardware) { hardware.Update(); foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this); }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}