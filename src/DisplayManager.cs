using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LuminaColor {
    public class DisplayResolutionMode {
        public int Width { get; set; }
        public int Height { get; set; }
        public int RefreshRate { get; set; }
        public int BitsPerPixel { get; set; }

        public string AspectRatio {
            get {
                int gcd = GCD(Width, Height);
                int num = Width / gcd;
                int den = Height / gcd;
                if (num == 16 && den == 9) return "16:9";
                if (num == 16 && den == 10) return "16:10";
                if (num == 4 && den == 3) return "4:3";
                if (num == 21 && den == 9) return "21:9";
                if (num == 32 && den == 9) return "32:9";
                return string.Format("{0}:{1}", num, den);
            }
        }

        public string DisplayText {
            get {
                string tag = "";
                if (Width >= 3840 && Height >= 2160) tag = " [4K UHD]";
                else if (Width >= 2560 && Height >= 1440) tag = " [2K QHD]";
                else if (Width == 1920 && Height == 1080) tag = " [Full HD]";
                return string.Format("{0} x {1} @ {2}Hz ({3}){4}", Width, Height, RefreshRate, AspectRatio, tag);
            }
        }

        private static int GCD(int a, int b) {
            while (b != 0) {
                int temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        public override bool Equals(object obj) {
            DisplayResolutionMode other = obj as DisplayResolutionMode;
            if (other == null) return false;
            return Width == other.Width && Height == other.Height && RefreshRate == other.RefreshRate;
        }

        public override int GetHashCode() {
            return (Width * 397) ^ (Height * 17) ^ RefreshRate;
        }
    }

    public class DisplayInfo {
        public string DeviceName { get; set; }
        public string FriendlyName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsExternal { get; set; }
        public bool HasDdcCi { get; set; }
        public IntPtr HMonitor { get; set; }
        public ushort[] OriginalRamp { get; set; }

        public string DisplayTitle {
            get {
                string tag = IsExternal ? "HDMI / External TV" : "Primary Display";
                return string.Format("{0} [{1}]", DeviceName, tag);
            }
        }

        public string ResolutionString {
            get {
                return string.Format("{0} x {1}", Width, Height);
            }
        }
    }

    public class DisplayManager {
        public List<DisplayInfo> Displays { get; private set; }

        public DisplayManager() {
            Displays = new List<DisplayInfo>();
            RefreshDisplays();
        }

        public void RefreshDisplays() {
            Displays.Clear();

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate(IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.RECT lprcMonitor, IntPtr dwData) {
                NativeMethods.MONITORINFOEX mi = new NativeMethods.MONITORINFOEX();
                mi.cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFOEX));
                if (NativeMethods.GetMonitorInfo(hMonitor, ref mi)) {
                    bool isPrimary = (mi.dwFlags & 1) != 0;
                    int width = lprcMonitor.Width;
                    int height = lprcMonitor.Height;

                    // External if not primary, or if display number > 1
                    bool isExternal = !isPrimary;

                    string friendly = isExternal ? "External HDMI Monitor / TV" : "Internal / Primary Display";

                    // Check DDC/CI
                    bool ddcSupported = false;
                    uint numPhys = 0;
                    if (NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out numPhys) && numPhys > 0) {
                        NativeMethods.PHYSICAL_MONITOR[] phys = new NativeMethods.PHYSICAL_MONITOR[numPhys];
                        if (NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, numPhys, phys)) {
                            if (!string.IsNullOrEmpty(phys[0].szPhysicalMonitorDescription)) {
                                friendly = phys[0].szPhysicalMonitorDescription;
                                if (friendly == "Generic PnP Monitor" && isExternal) {
                                    friendly = "HDMI TV / External Monitor";
                                }
                            }
                            uint pvct, cur, max;
                            ddcSupported = NativeMethods.GetVCPFeatureAndVCPFeatureReply(phys[0].hPhysicalMonitor, 0x10, out pvct, out cur, out max);
                            NativeMethods.DestroyPhysicalMonitors(numPhys, phys);
                        }
                    }

                    // Read original gamma ramp
                    ushort[] origRamp = new ushort[3 * 256];
                    IntPtr hdc = NativeMethods.CreateDC(mi.szDevice, mi.szDevice, null, IntPtr.Zero);
                    if (hdc != IntPtr.Zero) {
                        if (!NativeMethods.GetDeviceGammaRamp(hdc, origRamp)) {
                            origRamp = ColorEngine.BuildIdentityRamp();
                        }
                        NativeMethods.DeleteDC(hdc);
                    } else {
                        origRamp = ColorEngine.BuildIdentityRamp();
                    }

                    Displays.Add(new DisplayInfo {
                        DeviceName = mi.szDevice,
                        FriendlyName = friendly,
                        Width = width,
                        Height = height,
                        IsPrimary = isPrimary,
                        IsExternal = isExternal,
                        HasDdcCi = ddcSupported,
                        HMonitor = hMonitor,
                        OriginalRamp = origRamp
                    });
                }
                return true;
            }, IntPtr.Zero);

            if (Displays.Count > 1) {
                foreach (var d in Displays) {
                    if (!d.IsPrimary) d.IsExternal = true;
                }
            }
        }

        public DisplayResolutionMode GetCurrentResolution(string deviceName) {
            NativeMethods.DEVMODE dm = new NativeMethods.DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(NativeMethods.DEVMODE));
            if (NativeMethods.EnumDisplaySettings(deviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref dm)) {
                return new DisplayResolutionMode {
                    Width = dm.dmPelsWidth,
                    Height = dm.dmPelsHeight,
                    RefreshRate = dm.dmDisplayFrequency,
                    BitsPerPixel = dm.dmBitsPerPel
                };
            }
            return null;
        }

        public List<DisplayResolutionMode> GetSupportedResolutions(string deviceName) {
            List<DisplayResolutionMode> list = new List<DisplayResolutionMode>();
            HashSet<string> seen = new HashSet<string>();

            int modeNum = 0;
            NativeMethods.DEVMODE dm = new NativeMethods.DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(NativeMethods.DEVMODE));

            while (NativeMethods.EnumDisplaySettings(deviceName, modeNum, ref dm)) {
                if (dm.dmBitsPerPel >= 32 && dm.dmPelsWidth >= 800 && dm.dmPelsHeight >= 600) {
                    string key = string.Format("{0}x{1}@{2}", dm.dmPelsWidth, dm.dmPelsHeight, dm.dmDisplayFrequency);
                    if (!seen.Contains(key)) {
                        seen.Add(key);
                        list.Add(new DisplayResolutionMode {
                            Width = dm.dmPelsWidth,
                            Height = dm.dmPelsHeight,
                            RefreshRate = dm.dmDisplayFrequency,
                            BitsPerPixel = dm.dmBitsPerPel
                        });
                    }
                }
                modeNum++;
            }

            // Sort descending: highest resolution & refresh rate first
            list.Sort(delegate(DisplayResolutionMode a, DisplayResolutionMode b) {
                long areaA = (long)a.Width * a.Height;
                long areaB = (long)b.Width * b.Height;
                if (areaA != areaB) return areaB.CompareTo(areaA);
                return b.RefreshRate.CompareTo(a.RefreshRate);
            });

            return list;
        }

        public bool ChangeResolution(string deviceName, int width, int height, int refreshRate) {
            NativeMethods.DEVMODE dm = new NativeMethods.DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(NativeMethods.DEVMODE));
            dm.dmPelsWidth = width;
            dm.dmPelsHeight = height;
            dm.dmDisplayFrequency = refreshRate;
            dm.dmFields = NativeMethods.DM_PELSWIDTH | NativeMethods.DM_PELSHEIGHT;
            if (refreshRate > 0) {
                dm.dmFields |= NativeMethods.DM_DISPLAYFREQUENCY;
            }

            int res = NativeMethods.ChangeDisplaySettingsEx(deviceName, ref dm, IntPtr.Zero, NativeMethods.CDS_UPDATEREGISTRY, IntPtr.Zero);
            if (res == NativeMethods.DISP_CHANGE_SUCCESSFUL) {
                // Update local cached display info
                foreach (var d in Displays) {
                    if (d.DeviceName == deviceName) {
                        d.Width = width;
                        d.Height = height;
                        break;
                    }
                }
                return true;
            }
            return false;
        }

        public bool ApplyRamp(string deviceName, ushort[] ramp) {
            if (ramp == null || ramp.Length != 3 * 256) return false;

            IntPtr hdc = NativeMethods.CreateDC(deviceName, deviceName, null, IntPtr.Zero);
            if (hdc == IntPtr.Zero) return false;

            bool success = NativeMethods.SetDeviceGammaRamp(hdc, ramp);
            NativeMethods.DeleteDC(hdc);
            return success;
        }

        public void ResetDisplay(string deviceName) {
            foreach (var d in Displays) {
                if (d.DeviceName == deviceName) {
                    ApplyRamp(deviceName, d.OriginalRamp ?? ColorEngine.BuildIdentityRamp());
                    return;
                }
            }
            ApplyRamp(deviceName, ColorEngine.BuildIdentityRamp());
        }

        public void ResetAllDisplays() {
            foreach (var d in Displays) {
                ApplyRamp(d.DeviceName, d.OriginalRamp ?? ColorEngine.BuildIdentityRamp());
            }
        }
    }
}
