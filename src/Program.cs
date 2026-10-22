using System;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace LuminaColor {
    public class App : Application {
        private NotifyIcon _notifyIcon;
        private MainWindow _mainWindow;
        private DisplayManager _displayManager;
        private AppSettings _settings;
        private static Mutex _mutex = null;

        [STAThread]
        public static void Main(string[] args) {
            // Enable Per-Monitor High-DPI Awareness
            try {
                NativeMethods.SetProcessDpiAwareness(2); // Per-Monitor V2
            } catch {
                try {
                    NativeMethods.SetProcessDPIAware();
                } catch { }
            }

            // Single instance check
            bool createdNew;
            _mutex = new Mutex(true, "LuminaColor_SingleInstance_Mutex", out createdNew);
            if (!createdNew) {
                MessageBox.Show("LuminaColor is already running in your system tray.", "LuminaColor", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool startInTray = false;
            foreach (string arg in args) {
                if (arg.Equals("--tray", StringComparison.OrdinalIgnoreCase)) {
                    startInTray = true;
                }
            }

            App app = new App();
            app.RunApp(startInTray);
        }

        private void RunApp(bool startInTray) {
            _displayManager = new DisplayManager();
            _settings = SettingsManager.LoadSettings();

            // Apply saved profiles to all displays on startup
            foreach (var d in _displayManager.Displays) {
                DisplayProfile prof;
                if (_settings.DisplayProfiles.TryGetValue(d.DeviceName, out prof)) {
                    ushort[] ramp = ColorEngine.BuildGammaRamp(
                        prof.Kelvin,
                        prof.RedGain,
                        prof.GreenGain,
                        prof.BlueGain,
                        prof.Tint,
                        prof.Brightness,
                        prof.Contrast,
                        prof.Gamma,
                        prof.NoiseReduction,
                        prof.Clarity,
                        prof.Saturation,
                        prof.Vibrance,
                        prof.ShadowLift,
                        prof.HighlightRolloff
                    );
                    _displayManager.ApplyRamp(d.DeviceName, ramp);
                }
            }

            _mainWindow = new MainWindow(_displayManager, _settings);

            // Handle window closing -> minimize to tray
            _mainWindow.Closing += (s, e) => {
                e.Cancel = true;
                _mainWindow.Hide();
                if (_notifyIcon != null) {
                    _notifyIcon.ShowBalloonTip(2000, "LuminaColor Active", "LuminaColor is maintaining your HDMI & monitor calibration in the background.", ToolTipIcon.Info);
                }
            };

            SetupTrayIcon();

            if (!startInTray) {
                _mainWindow.Show();
            }

            Run();
        }

        private void SetupTrayIcon() {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "LuminaColor - Top-Notch HDMI Monitor & TV Studio";

            // Create a crisp tray icon programmatically
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp)) {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);
                using (Brush b = new SolidBrush(System.Drawing.Color.FromArgb(99, 102, 241))) {
                    g.FillEllipse(b, 2, 2, 28, 28);
                }
                using (Brush sun = new SolidBrush(System.Drawing.Color.FromArgb(251, 191, 36))) {
                    g.FillEllipse(sun, 9, 9, 14, 14);
                }
            }
            _notifyIcon.Icon = System.Drawing.Icon.FromHandle(bmp.GetHicon());
            _notifyIcon.Visible = true;

            // Context Menu
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem itemOpen = new ToolStripMenuItem("Open LuminaColor Studio");
            itemOpen.Font = new System.Drawing.Font(itemOpen.Font, System.Drawing.FontStyle.Bold);
            itemOpen.Click += (s, e) => RestoreMainWindow();
            menu.Items.Add(itemOpen);

            menu.Items.Add(new ToolStripSeparator());

            // Quick Presets Submenu
            ToolStripMenuItem menuPresets = new ToolStripMenuItem("Quick Presets");
            menuPresets.DropDownItems.Add("🎯 Perfect Match (HP OMEN)", null, (s, e) => ApplyQuickPreset("Perfect Match (HP OMEN)", 6300, 0.99, 0.99, 0.88, 0, 1.00, 1.02, 1.03, 0.0, 0.25));
            menuPresets.DropDownItems.Add("🎯 Match Laptop (Open Shadows)", null, (s, e) => ApplyQuickPreset("Match Laptop (Open Shadows)", 5000, 1.04, 1.02, 0.70, 8, 1.02, 0.98, 1.12, 0.0, 0.20));
            menuPresets.DropDownItems.Add("💻 Match Laptop (Deep Anti-Blue)", null, (s, e) => ApplyQuickPreset("Match Laptop (Deep Anti-Blue)", 4800, 1.05, 1.02, 0.65, 10, 1.04, 0.96, 1.15, 0.0, 0.15));
            menuPresets.DropDownItems.Add("🌟 New World Master OLED", null, (s, e) => ApplyQuickPreset("New World Master OLED", 5500, 1.0, 1.0, 0.95, -2, 1.0, 1.15, 1.0, 0.65, 0.50));
            menuPresets.DropDownItems.Add("💎 Crystal 4K Detail", null, (s, e) => ApplyQuickPreset("Crystal 4K Detail", 6500, 1.0, 1.0, 1.0, 0, 1.05, 1.10, 1.0, 0.30, 0.75));
            menuPresets.DropDownItems.Add("🎬 Cinema Warm (5400K)", null, (s, e) => ApplyQuickPreset("Cinema Warm", 5400, 1.0, 1.0, 0.92, -2, 1.0, 1.10, 1.02, 0.40, 0.30));
            menuPresets.DropDownItems.Add("🌙 Night Owl (2700K)", null, (s, e) => ApplyQuickPreset("Night Owl", 2700, 1.0, 0.95, 0.70, 0, 0.90, 1.0, 1.0, 0.50, 0.20));
            menuPresets.DropDownItems.Add("🎮 Gaming & Vivid", null, (s, e) => ApplyQuickPreset("Gaming & Vivid", 6500, 1.05, 1.0, 1.02, 0, 1.05, 1.15, 1.05, 0.20, 0.60));
            menuPresets.DropDownItems.Add("🖥️ sRGB Reference (6500K)", null, (s, e) => ApplyQuickPreset("sRGB Reference", 6500, 1.0, 1.0, 1.0, 0, 1.0, 1.0, 1.0, 0.0, 0.0));
            menuPresets.DropDownItems.Add("🕶️ Midnight Ultra-Dim", null, (s, e) => ApplyQuickPreset("Midnight Ultra-Dim", 2200, 1.0, 0.90, 0.60, 0, 0.40, 0.95, 0.95, 0.85, 0.10));
            menu.Items.Add(menuPresets);

            ToolStripMenuItem itemReset = new ToolStripMenuItem("↺ Reset All Displays to Factory Default");
            itemReset.Click += (s, e) => {
                _displayManager.ResetAllDisplays();
                if (_notifyIcon != null) {
                    _notifyIcon.ShowBalloonTip(1500, "Colors Reset", "All displays reset to factory calibration.", ToolTipIcon.Info);
                }
            };
            menu.Items.Add(itemReset);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem itemExit = new ToolStripMenuItem("Exit LuminaColor");
            itemExit.Click += (s, e) => {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                Environment.Exit(0);
            };
            menu.Items.Add(itemExit);

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (s, e) => RestoreMainWindow();
            _notifyIcon.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Left) {
                    RestoreMainWindow();
                }
            };
        }

        private void RestoreMainWindow() {
            if (_mainWindow != null) {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
                _mainWindow.Focus();
            }
        }

        private void ApplyQuickPreset(
            string name,
            int kelvin,
            double rGain,
            double gGain,
            double bGain,
            double tint,
            double brightness,
            double contrast,
            double gamma,
            double noiseReduction,
            double clarity,
            double saturation = 1.0,
            double vibrance = 1.0,
            double shadowLift = 0.0,
            double highlightRolloff = 0.0)
        {
            ushort[] ramp = ColorEngine.BuildGammaRamp(kelvin, rGain, gGain, bGain, tint, brightness, contrast, gamma, noiseReduction, clarity, saturation, vibrance, shadowLift, highlightRolloff);
            foreach (var d in _displayManager.Displays) {
                _displayManager.ApplyRamp(d.DeviceName, ramp);
            }
            if (_notifyIcon != null) {
                _notifyIcon.ShowBalloonTip(1500, "Preset Activated", "Applied '" + name + "' across displays.", ToolTipIcon.Info);
            }
        }

        protected override void OnExit(ExitEventArgs e) {
            if (_notifyIcon != null) {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            base.OnExit(e);
        }
    }
}
