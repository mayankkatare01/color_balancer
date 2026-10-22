using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace LuminaColor {
    public class DisplayProfile {
        public int Kelvin { get; set; }
        public double RedGain { get; set; }
        public double GreenGain { get; set; }
        public double BlueGain { get; set; }
        public double Tint { get; set; }
        public double Brightness { get; set; }
        public double Contrast { get; set; }
        public double Gamma { get; set; }
        public double NoiseReduction { get; set; }
        public double Clarity { get; set; }
        public double Saturation { get; set; }
        public double Vibrance { get; set; }
        public double ShadowLift { get; set; }
        public double HighlightRolloff { get; set; }
        public string ActivePresetName { get; set; }

        public DisplayProfile() {
            Kelvin = 6500;
            RedGain = 1.0;
            GreenGain = 1.0;
            BlueGain = 1.0;
            Tint = 0.0;
            Brightness = 1.0;
            Contrast = 1.0;
            Gamma = 1.0;
            NoiseReduction = 0.0;
            Clarity = 0.0;
            Saturation = 1.0;
            Vibrance = 1.0;
            ShadowLift = 0.0;
            HighlightRolloff = 0.0;
            ActivePresetName = "Neutral sRGB";
        }

        public DisplayProfile Clone() {
            return new DisplayProfile {
                Kelvin = this.Kelvin,
                RedGain = this.RedGain,
                GreenGain = this.GreenGain,
                BlueGain = this.BlueGain,
                Tint = this.Tint,
                Brightness = this.Brightness,
                Contrast = this.Contrast,
                Gamma = this.Gamma,
                NoiseReduction = this.NoiseReduction,
                Clarity = this.Clarity,
                Saturation = this.Saturation,
                Vibrance = this.Vibrance,
                ShadowLift = this.ShadowLift,
                HighlightRolloff = this.HighlightRolloff,
                ActivePresetName = this.ActivePresetName
            };
        }
    }

    public class AppSettings {
        public string SelectedDevice { get; set; }
        public bool SyncAllDisplays { get; set; }
        public bool StartWithWindows { get; set; }
        public Dictionary<string, DisplayProfile> DisplayProfiles { get; set; }
        public Dictionary<string, DisplayProfile> CustomPresets { get; set; }

        public AppSettings() {
            SelectedDevice = "";
            SyncAllDisplays = false;
            StartWithWindows = false;
            DisplayProfiles = new Dictionary<string, DisplayProfile>();
            CustomPresets = new Dictionary<string, DisplayProfile>();
        }
    }

    public static class SettingsManager {
        private static string SettingsFolder {
            get {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dir = Path.Combine(appData, "LuminaColor");
                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
                return dir;
            }
        }

        private static string SettingsFilePath {
            get {
                return Path.Combine(SettingsFolder, "config.ini");
            }
        }

        public static AppSettings LoadSettings() {
            AppSettings settings = new AppSettings();
            string path = SettingsFilePath;
            if (!File.Exists(path)) {
                return settings;
            }

            try {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                string currentSection = "";

                foreach (string rawLine in lines) {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith(";")) continue;

                    if (line.StartsWith("[") && line.EndsWith("]")) {
                        currentSection = line.Substring(1, line.Length - 2).Trim();
                        continue;
                    }

                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    string key = line.Substring(0, idx).Trim();
                    string val = line.Substring(idx + 1).Trim();

                    if (currentSection == "General") {
                        if (key == "SelectedDevice") settings.SelectedDevice = val;
                        else if (key == "SyncAllDisplays") settings.SyncAllDisplays = (val == "1" || val.ToLower() == "true");
                        else if (key == "StartWithWindows") settings.StartWithWindows = (val == "1" || val.ToLower() == "true");
                    } else if (currentSection.StartsWith("Display_")) {
                        string device = currentSection.Substring("Display_".Length).Replace("_SLASH_", "\\");
                        if (!settings.DisplayProfiles.ContainsKey(device)) {
                            settings.DisplayProfiles[device] = new DisplayProfile();
                        }
                        ApplyProperty(settings.DisplayProfiles[device], key, val);
                    } else if (currentSection.StartsWith("CustomPreset_")) {
                        string presetName = currentSection.Substring("CustomPreset_".Length);
                        if (!settings.CustomPresets.ContainsKey(presetName)) {
                            settings.CustomPresets[presetName] = new DisplayProfile();
                        }
                        ApplyProperty(settings.CustomPresets[presetName], key, val);
                    }
                }
            } catch {
                // Return default settings on parsing error
            }

            return settings;
        }

        private static void ApplyProperty(DisplayProfile prof, string key, string val) {
            double d;
            int i;
            if (key == "Kelvin" && int.TryParse(val, out i)) prof.Kelvin = i;
            else if (key == "RedGain" && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) prof.RedGain = d;
            else if (key == "GreenGain" && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) prof.GreenGain = d;
            else if (key == "BlueGain" && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) prof.BlueGain = d;
            else if (key == "Tint" && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) prof.Tint = d;
            else if (key == "Brightness" && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) prof.Brightness = d;
            else if (key == "Contrast" && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) prof.Contrast = d;
            else if (key == "Gamma" && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) prof.Gamma = d;
            else if (key == "NoiseReduction" && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) prof.NoiseReduction = d;
            else if (key == "Clarity" && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) prof.Clarity = d;
            else if (key == "Saturation" && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) prof.Saturation = d;
            else if (key == "Vibrance" && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) prof.Vibrance = d;
            else if (key == "ShadowLift" && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) prof.ShadowLift = d;
            else if (key == "HighlightRolloff" && double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d)) prof.HighlightRolloff = d;
            else if (key == "ActivePresetName") prof.ActivePresetName = val;
        }

        public static void SaveSettings(AppSettings settings) {
            try {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("[General]");
                sb.AppendLine("SelectedDevice=" + settings.SelectedDevice);
                sb.AppendLine("SyncAllDisplays=" + (settings.SyncAllDisplays ? "1" : "0"));
                sb.AppendLine("StartWithWindows=" + (settings.StartWithWindows ? "1" : "0"));
                sb.AppendLine();

                foreach (var kvp in settings.DisplayProfiles) {
                    string safeSection = "Display_" + kvp.Key.Replace("\\", "_SLASH_");
                    sb.AppendLine("[" + safeSection + "]");
                    WriteProfile(sb, kvp.Value);
                    sb.AppendLine();
                }

                foreach (var kvp in settings.CustomPresets) {
                    sb.AppendLine("[CustomPreset_" + kvp.Key + "]");
                    WriteProfile(sb, kvp.Value);
                    sb.AppendLine();
                }

                File.WriteAllText(SettingsFilePath, sb.ToString(), Encoding.UTF8);
            } catch {
                // Ignore save errors
            }
        }

        private static void WriteProfile(StringBuilder sb, DisplayProfile p) {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            sb.AppendLine("Kelvin=" + p.Kelvin);
            sb.AppendLine("RedGain=" + p.RedGain.ToString("0.000", ci));
            sb.AppendLine("GreenGain=" + p.GreenGain.ToString("0.000", ci));
            sb.AppendLine("BlueGain=" + p.BlueGain.ToString("0.000", ci));
            sb.AppendLine("Tint=" + p.Tint.ToString("0.000", ci));
            sb.AppendLine("Brightness=" + p.Brightness.ToString("0.000", ci));
            sb.AppendLine("Contrast=" + p.Contrast.ToString("0.000", ci));
            sb.AppendLine("Gamma=" + p.Gamma.ToString("0.000", ci));
            sb.AppendLine("NoiseReduction=" + p.NoiseReduction.ToString("0.000", ci));
            sb.AppendLine("Clarity=" + p.Clarity.ToString("0.000", ci));
            sb.AppendLine("Saturation=" + p.Saturation.ToString("0.000", ci));
            sb.AppendLine("Vibrance=" + p.Vibrance.ToString("0.000", ci));
            sb.AppendLine("ShadowLift=" + p.ShadowLift.ToString("0.000", ci));
            sb.AppendLine("HighlightRolloff=" + p.HighlightRolloff.ToString("0.000", ci));
            sb.AppendLine("ActivePresetName=" + (p.ActivePresetName ?? ""));
        }

        public static void SetStartupRegistration(bool enable) {
            try {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)) {
                    if (key != null) {
                        string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        if (enable) {
                            key.SetValue("LuminaColor", "\"" + appPath + "\" --tray");
                        } else {
                            key.DeleteValue("LuminaColor", false);
                        }
                    }
                }
            } catch {
            }
        }
    }
}
