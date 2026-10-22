using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LuminaColor {
    public class MainWindow : Window {
        private DisplayManager _displayManager;
        private AppSettings _settings;
        private DisplayInfo _currentDisplay;
        private bool _isComparingOriginal = false;
        private ushort[] _activeCustomRamp = null;
        private DispatcherTimer _applyThrottleTimer;
        private bool _pendingApply = false;

        // UI Controls - Displays & Resolution
        private StackPanel _displaysPanel;
        private TextBlock _txtSelectedDisplayName;
        private TextBlock _txtSelectedDisplayMeta;
        private ComboBox _cboResolution;
        private Button _btnApplyResolution;
        private List<DisplayResolutionMode> _currentSupportedModes;

        // UI Controls - Warmth & Tint
        private Slider _sliderKelvin;
        private TextBlock _lblKelvinVal;
        private TextBlock _lblKelvinDesc;
        private Border _swatchKelvin;
        private Slider _sliderTint;
        private TextBlock _lblTintVal;

        // UI Controls - RGB Channels
        private Slider _sliderRed;
        private TextBlock _lblRedVal;
        private Slider _sliderGreen;
        private TextBlock _lblGreenVal;
        private Slider _sliderBlue;
        private TextBlock _lblBlueVal;

        // UI Controls - New World Clarity & Noise Reduction
        private Slider _sliderNoiseReduction;
        private TextBlock _lblNoiseVal;
        private Slider _sliderClarity;
        private TextBlock _lblClarityVal;

        // UI Controls - Dynamics
        private Slider _sliderBrightness;
        private TextBlock _lblBrightnessVal;
        private Slider _sliderContrast;
        private TextBlock _lblContrastVal;
        private Slider _sliderGamma;
        private TextBlock _lblGammaVal;

        // UI Controls - Saturation & Vibrance
        private Slider _sliderSaturation;
        private TextBlock _lblSaturationVal;
        private Slider _sliderVibrance;
        private TextBlock _lblVibranceVal;

        // UI Controls - Shadows & Highlights
        private Slider _sliderShadowLift;
        private TextBlock _lblShadowLiftVal;
        private Slider _sliderHighlightRolloff;
        private TextBlock _lblHighlightRolloffVal;

        // Header & Presets
        private Button _btnCompare;
        private CheckBox _chkSyncAll;
        private CheckBox _chkStartup;
        private WrapPanel _presetsPanel;
        private ComboBox _cboCustomPresets;
        private TextBox _txtCustomPresetName;
        private TextBlock _txtStatus;

        // Brushes & Styling
        private readonly System.Windows.Media.Brush _bgBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(11, 15, 25));
        private readonly System.Windows.Media.Brush _cardBgBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(19, 27, 46));
        private readonly System.Windows.Media.Brush _cardBorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 44, 69));
        private readonly System.Windows.Media.Brush _accentBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(99, 102, 241));
        private readonly System.Windows.Media.Brush _textPrimary = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 244, 246));
        private readonly System.Windows.Media.Brush _textSecondary = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
        private readonly System.Windows.Media.Brush _textMuted = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128));

        public MainWindow(DisplayManager displayManager, AppSettings settings) {
            _displayManager = displayManager;
            _settings = settings;

            Title = "LuminaColor - Top-Notch Windows 11 Monitor & TV Studio";
            Width = 1080;
            Height = 880;
            MinWidth = 960;
            MinHeight = 720;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = _bgBrush;
            Foreground = _textPrimary;
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI Variable Display, Segoe UI, sans-serif");

            _applyThrottleTimer = new DispatcherTimer();
            _applyThrottleTimer.Interval = TimeSpan.FromMilliseconds(16); // 60 FPS update rate
            _applyThrottleTimer.Tick += (s, e) => {
                _applyThrottleTimer.Stop();
                if (_pendingApply) {
                    _pendingApply = false;
                    ApplyCurrentSettingsCore();
                }
            };

            BuildUI();
            SelectInitialDisplay();
        }

        private void BuildUI() {
            Grid rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) }); // Header
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Main Content
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) }); // Status Footer

            // --- HEADER ---
            Border headerBorder = new Border {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 15, 21, 37)),
                BorderBrush = _cardBorderBrush,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(24, 0, 24, 0)
            };

            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            // Brand Logo & Title
            StackPanel brandPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Border logoBorder = new Border {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(10),
                Background = new LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(99, 102, 241),
                    System.Windows.Media.Color.FromRgb(6, 182, 212),
                    45.0
                ),
                Margin = new Thickness(0, 0, 12, 0)
            };
            TextBlock logoText = new TextBlock {
                Text = "☀",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            logoBorder.Child = logoText;
            brandPanel.Children.Add(logoBorder);

            StackPanel titleTextPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            TextBlock titleBlock = new TextBlock {
                Text = "LuminaColor Studio",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = _textPrimary
            };
            TextBlock subTitleBlock = new TextBlock {
                Text = "Top-Notch HDMI Monitor & TV Engine • Denoise & Resolution",
                FontSize = 11,
                Foreground = _textSecondary
            };
            titleTextPanel.Children.Add(titleBlock);
            titleTextPanel.Children.Add(subTitleBlock);
            brandPanel.Children.Add(titleTextPanel);
            Grid.SetColumn(brandPanel, 0);
            headerGrid.Children.Add(brandPanel);

            // Header Right Controls (A/B Compare, Reset, Minimize)
            StackPanel headerRightPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            _btnCompare = CreateActionButton("⚡ A/B Compare (Original)", System.Windows.Media.Color.FromRgb(30, 41, 59));
            _btnCompare.Margin = new Thickness(0, 0, 10, 0);
            _btnCompare.Click += (s, e) => ToggleCompareOriginal();
            headerRightPanel.Children.Add(_btnCompare);

            Button btnReset = CreateActionButton("↺ Reset All", System.Windows.Media.Color.FromRgb(30, 41, 59));
            btnReset.Margin = new Thickness(0, 0, 10, 0);
            btnReset.Click += (s, e) => ResetCurrentDisplay();
            headerRightPanel.Children.Add(btnReset);

            Button btnMinimize = CreateActionButton("🗕 Minimize to Tray", System.Windows.Media.Color.FromRgb(20, 26, 40));
            btnMinimize.Click += (s, e) => Hide();
            headerRightPanel.Children.Add(btnMinimize);

            Grid.SetColumn(headerRightPanel, 2);
            headerGrid.Children.Add(headerRightPanel);
            headerBorder.Child = headerGrid;
            Grid.SetRow(headerBorder, 0);
            rootGrid.Children.Add(headerBorder);

            // --- MAIN CONTENT AREA (Scrollable) ---
            ScrollViewer scroll = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(24, 20, 24, 20)
            };

            StackPanel contentStack = new StackPanel();

            // 1. MONITOR SELECTOR & RESOLUTION CONTROLS
            contentStack.Children.Add(BuildMonitorSelectorAndResolutionSection());

            // 2. MAIN CALIBRATION GRID (Two Columns)
            Grid calibGrid = new Grid { Margin = new Thickness(0, 16, 0, 0) };
            calibGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) });
            calibGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) }); // Gap
            calibGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left Col: Warmth & Tint & Top-Notch Noise/Clarity Engine
            Border leftColCard = CreateCard();
            leftColCard.Child = BuildWarmthAndNoisePanel();
            Grid.SetColumn(leftColCard, 0);
            calibGrid.Children.Add(leftColCard);

            // Right Col: RGB Channels & Dynamics
            Border rightColCard = CreateCard();
            rightColCard.Child = BuildRgbAndDynamicsPanel();
            Grid.SetColumn(rightColCard, 2);
            calibGrid.Children.Add(rightColCard);

            contentStack.Children.Add(calibGrid);

            // 3. CURATED PRESETS & CUSTOM PROFILES
            contentStack.Children.Add(BuildPresetsSection());

            scroll.Content = contentStack;
            Grid.SetRow(scroll, 1);
            rootGrid.Children.Add(scroll);

            // --- STATUS FOOTER ---
            Border footerBorder = new Border {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 21, 37)),
                BorderBrush = _cardBorderBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 0, 24, 0)
            };

            Grid footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            _txtStatus = new TextBlock {
                Text = "⚡ Top-Notch Display Processing Active • Hardware GPU GDI • 60 FPS Response",
                FontSize = 12,
                Foreground = _textSecondary,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_txtStatus, 0);
            footerGrid.Children.Add(_txtStatus);

            StackPanel footerRight = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _chkStartup = new CheckBox {
                Content = "Start minimized with Windows",
                Foreground = _textSecondary,
                IsChecked = _settings.StartWithWindows,
                VerticalAlignment = VerticalAlignment.Center
            };
            _chkStartup.Checked += (s, e) => {
                _settings.StartWithWindows = true;
                SettingsManager.SetStartupRegistration(true);
                SettingsManager.SaveSettings(_settings);
            };
            _chkStartup.Unchecked += (s, e) => {
                _settings.StartWithWindows = false;
                SettingsManager.SetStartupRegistration(false);
                SettingsManager.SaveSettings(_settings);
            };
            footerRight.Children.Add(_chkStartup);
            Grid.SetColumn(footerRight, 1);
            footerGrid.Children.Add(footerRight);

            footerBorder.Child = footerGrid;
            Grid.SetRow(footerBorder, 2);
            rootGrid.Children.Add(footerBorder);

            Content = rootGrid;
        }

        // ==========================================
        // MONITOR SELECTOR & RESOLUTION CONTROLS
        // ==========================================
        private UIElement BuildMonitorSelectorAndResolutionSection() {
            Border card = CreateCard();
            StackPanel sp = new StackPanel();

            // Header row with monitor cards and sync toggle
            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            StackPanel titlePanel = new StackPanel();
            TextBlock title = new TextBlock {
                Text = "CONNECTED DISPLAYS & HDMI MONITORS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(129, 140, 248)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            titlePanel.Children.Add(title);

            _txtSelectedDisplayName = new TextBlock {
                Text = "Select a monitor to calibrate",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = _textPrimary
            };
            titlePanel.Children.Add(_txtSelectedDisplayName);

            _txtSelectedDisplayMeta = new TextBlock {
                Text = "HDMI Display Calibration Mode",
                FontSize = 12,
                Foreground = _textSecondary
            };
            titlePanel.Children.Add(_txtSelectedDisplayMeta);

            Grid.SetColumn(titlePanel, 0);
            header.Children.Add(titlePanel);

            StackPanel optsPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _chkSyncAll = new CheckBox {
                Content = "Sync color to all displays",
                Foreground = _textPrimary,
                IsChecked = _settings.SyncAllDisplays,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 16, 0)
            };
            _chkSyncAll.Checked += (s, e) => { _settings.SyncAllDisplays = true; SettingsManager.SaveSettings(_settings); };
            _chkSyncAll.Unchecked += (s, e) => { _settings.SyncAllDisplays = false; SettingsManager.SaveSettings(_settings); };
            optsPanel.Children.Add(_chkSyncAll);

            Button btnRefresh = CreateActionButton("🔄 Refresh Displays", System.Windows.Media.Color.FromRgb(30, 41, 59));
            btnRefresh.Click += (s, e) => {
                _displayManager.RefreshDisplays();
                RebuildDisplayCards();
                SelectInitialDisplay();
            };
            optsPanel.Children.Add(btnRefresh);

            Grid.SetColumn(optsPanel, 1);
            header.Children.Add(optsPanel);
            sp.Children.Add(header);

            // Display Cards Grid
            _displaysPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 14, 0, 0) };
            RebuildDisplayCards();
            sp.Children.Add(_displaysPanel);

            // Separator line
            Border sep = new Border {
                Height = 1,
                Background = _cardBorderBrush,
                Margin = new Thickness(0, 14, 0, 14)
            };
            sp.Children.Add(sep);

            // RESOLUTION SELECTOR ROW
            Grid resGrid = new Grid();
            resGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            resGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            resGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            StackPanel resLabelCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 16, 0) };
            TextBlock resTitle = new TextBlock {
                Text = "DISPLAY RESOLUTION & REFRESH RATE",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(56, 189, 248))
            };
            TextBlock resSubtitle = new TextBlock {
                Text = "Set highest native crispness for your external HDMI monitor / TV",
                FontSize = 11,
                Foreground = _textSecondary
            };
            resLabelCol.Children.Add(resTitle);
            resLabelCol.Children.Add(resSubtitle);
            Grid.SetColumn(resLabelCol, 0);
            resGrid.Children.Add(resLabelCol);

            _cboResolution = new ComboBox {
                Height = 34,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 36, 62)),
                Foreground = _textPrimary,
                BorderBrush = _cardBorderBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            ApplyDarkComboBoxStyle(_cboResolution);
            Grid.SetColumn(_cboResolution, 1);
            resGrid.Children.Add(_cboResolution);

            _btnApplyResolution = CreateActionButton("Apply Resolution", System.Windows.Media.Color.FromRgb(14, 165, 233));
            _btnApplyResolution.Click += (s, e) => ApplySelectedResolution();
            Grid.SetColumn(_btnApplyResolution, 2);
            resGrid.Children.Add(_btnApplyResolution);

            sp.Children.Add(resGrid);

            // SYSTEM DISPLAY SHORTCUTS ROW
            StackPanel toolsRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
            
            Button btnNvidia = CreateActionButton("🎮 Open NVIDIA Control Panel", System.Windows.Media.Color.FromRgb(34, 45, 60));
            btnNvidia.FontSize = 11;
            btnNvidia.Margin = new Thickness(0, 0, 8, 0);
            btnNvidia.Click += (s, e) => {
                try {
                    Process.Start("explorer.exe", "shell:AppsFolder\\NVIDIACorp.NVIDIAControlPanel_56jybvy8sckqj!NVIDIACorp.NVIDIAControlPanel");
                } catch {
                    try {
                        Process.Start("explorer.exe", "shell:AppsFolder\\com.nvidia.nvapp");
                    } catch {
                        try { Process.Start("control.exe", "/name Microsoft.NvidiaControlPanel"); } catch { }
                    }
                }
            };
            toolsRow.Children.Add(btnNvidia);

            Button btnIntel = CreateActionButton("💻 Open Intel Graphics", System.Windows.Media.Color.FromRgb(34, 45, 60));
            btnIntel.FontSize = 11;
            btnIntel.Margin = new Thickness(0, 0, 8, 0);
            btnIntel.Click += (s, e) => {
                try {
                    Process.Start("explorer.exe", "shell:AppsFolder\\AppUp.IntelGraphicsExperience_8j3eq9eme6ctt!App");
                } catch { }
            };
            toolsRow.Children.Add(btnIntel);

            Button btnClearType = CreateActionButton("🔤 ClearType Text Calibrator", System.Windows.Media.Color.FromRgb(34, 45, 60));
            btnClearType.FontSize = 11;
            btnClearType.Margin = new Thickness(0, 0, 8, 0);
            btnClearType.Click += (s, e) => {
                try { Process.Start("cttune.exe"); } catch { }
            };
            toolsRow.Children.Add(btnClearType);

            Button btnWinDisp = CreateActionButton("⚙️ Windows Display Settings", System.Windows.Media.Color.FromRgb(34, 45, 60));
            btnWinDisp.FontSize = 11;
            btnWinDisp.Click += (s, e) => {
                try { Process.Start("ms-settings:display"); } catch { }
            };
            toolsRow.Children.Add(btnWinDisp);

            sp.Children.Add(toolsRow);

            card.Child = sp;
            return card;
        }

        private void RebuildDisplayCards() {
            _displaysPanel.Children.Clear();
            foreach (var d in _displayManager.Displays) {
                Border card = new Border {
                    Width = 240,
                    Height = 84,
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 34, 58)),
                    BorderBrush = (_currentDisplay != null && _currentDisplay.DeviceName == d.DeviceName) ? _accentBrush : _cardBorderBrush,
                    BorderThickness = new Thickness(2),
                    Margin = new Thickness(0, 0, 12, 0),
                    Padding = new Thickness(14, 10, 14, 10),
                    Cursor = Cursors.Hand,
                    Tag = d
                };

                Grid cardGrid = new Grid();
                cardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                cardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

                StackPanel topRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                TextBlock icon = new TextBlock {
                    Text = d.IsExternal ? "📺" : "💻",
                    FontSize = 18,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                topRow.Children.Add(icon);

                StackPanel textCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                TextBlock name = new TextBlock {
                    Text = d.FriendlyName,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13,
                    Foreground = _textPrimary,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxWidth = 160
                };
                textCol.Children.Add(name);

                TextBlock sub = new TextBlock {
                    Text = string.Format("{0} • {1}", d.DeviceName, d.ResolutionString),
                    FontSize = 11,
                    Foreground = _textSecondary
                };
                textCol.Children.Add(sub);
                topRow.Children.Add(textCol);
                Grid.SetRow(topRow, 0);
                cardGrid.Children.Add(topRow);

                // Bottom badge
                Border badge = new Border {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    Background = d.IsExternal
                        ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 6, 182, 212))
                        : new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 99, 102, 241))
                };
                TextBlock badgeTxt = new TextBlock {
                    Text = d.IsExternal ? "★ HDMI / External TV" : "Primary Display",
                    FontSize = 10,
                    FontWeight = FontWeights.Medium,
                    Foreground = d.IsExternal
                        ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 211, 238))
                        : new SolidColorBrush(System.Windows.Media.Color.FromRgb(165, 180, 252))
                };
                badge.Child = badgeTxt;
                Grid.SetRow(badge, 1);
                cardGrid.Children.Add(badge);

                card.Child = cardGrid;

                card.MouseEnter += (s, e) => {
                    if (_currentDisplay == null || _currentDisplay.DeviceName != d.DeviceName) {
                        card.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(32, 43, 72));
                    }
                };
                card.MouseLeave += (s, e) => {
                    if (_currentDisplay == null || _currentDisplay.DeviceName != d.DeviceName) {
                        card.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 34, 58));
                    }
                };
                card.MouseDown += (s, e) => {
                    SelectDisplay(d);
                };

                _displaysPanel.Children.Add(card);
            }
        }

        private void SelectInitialDisplay() {
            if (_displayManager.Displays.Count == 0) return;

            DisplayInfo target = null;
            if (!string.IsNullOrEmpty(_settings.SelectedDevice)) {
                target = _displayManager.Displays.Find(x => x.DeviceName == _settings.SelectedDevice);
            }
            if (target == null) {
                target = _displayManager.Displays.Find(x => x.IsExternal);
            }
            if (target == null) {
                target = _displayManager.Displays[0];
            }

            SelectDisplay(target);
        }

        private void SelectDisplay(DisplayInfo d) {
            _currentDisplay = d;
            _settings.SelectedDevice = d.DeviceName;
            SettingsManager.SaveSettings(_settings);

            _txtSelectedDisplayName.Text = string.Format("{0} ({1})", d.FriendlyName, d.DeviceName);
            _txtSelectedDisplayMeta.Text = string.Format("Current: {0} • Mode: {1} • Hardware DDC/CI: {2}",
                d.ResolutionString,
                d.IsExternal ? "External HDMI Monitor / TV" : "Internal Primary",
                d.HasDdcCi ? "Available" : "GPU Precision Mode"
            );

            // Highlight active card
            foreach (UIElement elem in _displaysPanel.Children) {
                Border b = elem as Border;
                if (b != null) {
                    DisplayInfo di = b.Tag as DisplayInfo;
                    if (di != null) {
                        if (di.DeviceName == d.DeviceName) {
                            b.BorderBrush = _accentBrush;
                            b.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(32, 45, 78));
                        } else {
                            b.BorderBrush = _cardBorderBrush;
                            b.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 34, 58));
                        }
                    }
                }
            }

            // Populate supported resolutions for this display
            PopulateResolutions(d.DeviceName);

            // Load profile for this display
            DisplayProfile prof;
            if (!_settings.DisplayProfiles.TryGetValue(d.DeviceName, out prof)) {
                prof = new DisplayProfile();
                _settings.DisplayProfiles[d.DeviceName] = prof;
            }

            LoadProfileToUI(prof);
        }

        private void PopulateResolutions(string deviceName) {
            _currentSupportedModes = _displayManager.GetSupportedResolutions(deviceName);
            DisplayResolutionMode current = _displayManager.GetCurrentResolution(deviceName);

            _cboResolution.Items.Clear();
            int selectedIdx = 0;

            for (int i = 0; i < _currentSupportedModes.Count; i++) {
                var m = _currentSupportedModes[i];
                bool isCur = current != null && m.Width == current.Width && m.Height == current.Height && m.RefreshRate == current.RefreshRate;
                string itemText = isCur ? string.Format("✓ {0} (Current Active)", m.DisplayText) : m.DisplayText;
                _cboResolution.Items.Add(itemText);
                if (isCur) {
                    selectedIdx = i;
                }
            }

            if (_cboResolution.Items.Count > 0) {
                _cboResolution.SelectedIndex = selectedIdx;
            }
        }

        private void ApplySelectedResolution() {
            if (_currentDisplay == null || _cboResolution.SelectedIndex < 0 || _currentSupportedModes == null || _cboResolution.SelectedIndex >= _currentSupportedModes.Count) {
                return;
            }

            DisplayResolutionMode target = _currentSupportedModes[_cboResolution.SelectedIndex];
            DisplayResolutionMode prev = _displayManager.GetCurrentResolution(_currentDisplay.DeviceName);

            bool ok = _displayManager.ChangeResolution(_currentDisplay.DeviceName, target.Width, target.Height, target.RefreshRate);
            if (ok) {
                _txtStatus.Text = string.Format("✓ Changed resolution to {0}x{1} @ {2}Hz", target.Width, target.Height, target.RefreshRate);
                RebuildDisplayCards();
                SelectDisplay(_currentDisplay);

                // Show safety confirmation countdown dialog
                ShowResolutionConfirmation(target, prev);
            } else {
                MessageBox.Show(
                    string.Format("Failed to switch {0} to {1}x{2}. The display driver rejected the video mode.", _currentDisplay.FriendlyName, target.Width, target.Height),
                    "Resolution Switch",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        private void ShowResolutionConfirmation(DisplayResolutionMode newMode, DisplayResolutionMode prevMode) {
            if (prevMode == null) return;

            Window dlg = new Window {
                Title = "Keep Display Settings?",
                Width = 420,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = _bgBrush,
                Foreground = _textPrimary,
                ResizeMode = ResizeMode.NoResize,
                Topmost = true
            };

            int remainingSeconds = 15;
            TextBlock lblCountdown = new TextBlock {
                Text = string.Format("Reverting in {0} seconds...", remainingSeconds),
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)),
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 16)
            };

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => {
                remainingSeconds--;
                if (remainingSeconds <= 0) {
                    timer.Stop();
                    // Revert
                    _displayManager.ChangeResolution(_currentDisplay.DeviceName, prevMode.Width, prevMode.Height, prevMode.RefreshRate);
                    RebuildDisplayCards();
                    PopulateResolutions(_currentDisplay.DeviceName);
                    dlg.Close();
                } else {
                    lblCountdown.Text = string.Format("Reverting in {0} seconds...", remainingSeconds);
                }
            };
            timer.Start();

            StackPanel sp = new StackPanel { Margin = new Thickness(24) };
            TextBlock lbl = new TextBlock {
                Text = string.Format("Resolution changed to {0}x{1} @ {2}Hz.", newMode.Width, newMode.Height, newMode.RefreshRate),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            sp.Children.Add(lbl);
            sp.Children.Add(lblCountdown);

            StackPanel btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Button btnKeep = CreateActionButton("✓ Keep Changes", System.Windows.Media.Color.FromRgb(16, 185, 129));
            btnKeep.Click += (s, e) => {
                timer.Stop();
                dlg.Close();
            };
            Button btnRevert = CreateActionButton("Revert Now", System.Windows.Media.Color.FromRgb(239, 68, 68));
            btnRevert.Margin = new Thickness(10, 0, 0, 0);
            btnRevert.Click += (s, e) => {
                timer.Stop();
                _displayManager.ChangeResolution(_currentDisplay.DeviceName, prevMode.Width, prevMode.Height, prevMode.RefreshRate);
                RebuildDisplayCards();
                PopulateResolutions(_currentDisplay.DeviceName);
                dlg.Close();
            };

            btnRow.Children.Add(btnKeep);
            btnRow.Children.Add(btnRevert);
            sp.Children.Add(btnRow);

            dlg.Content = sp;
            dlg.Closed += (s, e) => timer.Stop();
            dlg.ShowDialog();
        }

        // ==========================================
        // WARMTH, NOISE REDUCTION & CLARITY (LEFT COL)
        // ==========================================
        private UIElement BuildWarmthAndNoisePanel() {
            StackPanel sp = new StackPanel();

            // Panel Title
            TextBlock title = new TextBlock {
                Text = "WARMTH & COLOR TEMPERATURE",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(251, 191, 36)),
                Margin = new Thickness(0, 0, 0, 14)
            };
            sp.Children.Add(title);

            // Kelvin Swatch and Big Readout
            Border swatchBox = new Border {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 32, 54)),
                BorderBrush = _cardBorderBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 14)
            };

            Grid swatchGrid = new Grid();
            swatchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            swatchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            swatchGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _swatchKelvin = new Border {
                Width = 44,
                Height = 44,
                CornerRadius = new CornerRadius(22),
                BorderBrush = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(2),
                Background = System.Windows.Media.Brushes.White
            };
            Grid.SetColumn(_swatchKelvin, 0);
            swatchGrid.Children.Add(_swatchKelvin);

            StackPanel readoutPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _lblKelvinVal = new TextBlock {
                Text = "6500 K",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = _textPrimary
            };
            _lblKelvinDesc = new TextBlock {
                Text = "Pure Daylight (D65 Neutral)",
                FontSize = 12,
                Foreground = _textSecondary
            };
            readoutPanel.Children.Add(_lblKelvinVal);
            readoutPanel.Children.Add(_lblKelvinDesc);
            Grid.SetColumn(readoutPanel, 2);
            swatchGrid.Children.Add(readoutPanel);
            swatchBox.Child = swatchGrid;
            sp.Children.Add(swatchBox);

            // Kelvin Spectrum Bar Visual
            Border spectrumBar = new Border {
                Height = 10,
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 0, 0, 6),
                Background = new LinearGradientBrush(new GradientStopCollection {
                    new GradientStop(System.Windows.Media.Color.FromRgb(255, 140, 0), 0.0),
                    new GradientStop(System.Windows.Media.Color.FromRgb(255, 180, 80), 0.2),
                    new GradientStop(System.Windows.Media.Color.FromRgb(255, 230, 180), 0.45),
                    new GradientStop(System.Windows.Media.Color.FromRgb(255, 255, 255), 0.65),
                    new GradientStop(System.Windows.Media.Color.FromRgb(180, 215, 255), 1.0)
                }, new System.Windows.Point(0, 0), new System.Windows.Point(1, 0))
            };
            sp.Children.Add(spectrumBar);

            // Kelvin Slider
            _sliderKelvin = CreateStyledSlider(1500, 10000, 6500, 50);
            _sliderKelvin.ValueChanged += (s, e) => {
                int k = (int)_sliderKelvin.Value;
                _lblKelvinVal.Text = string.Format("{0} K", k);
                _lblKelvinDesc.Text = ColorEngine.GetKelvinDescription(k);
                UpdateSwatchColor(k);
                TriggerSettingsChanged();
            };
            sp.Children.Add(_sliderKelvin);

            // Kelvin Quick Presets Row
            WrapPanel quickKelvin = new WrapPanel { Margin = new Thickness(0, 6, 0, 14) };
            quickKelvin.Children.Add(CreateKelvinChip("2400K Candle", 2400));
            quickKelvin.Children.Add(CreateKelvinChip("3200K Sunset", 3200));
            quickKelvin.Children.Add(CreateKelvinChip("4500K Warm", 4500));
            quickKelvin.Children.Add(CreateKelvinChip("6500K Daylight", 6500));
            quickKelvin.Children.Add(CreateKelvinChip("8500K Cool", 8500));
            sp.Children.Add(quickKelvin);

            // TINT BALANCER
            Grid tintHeader = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            tintHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tintHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            TextBlock tintTitle = new TextBlock {
                Text = "TINT BALANCER (TV GREEN / MAGENTA FIX)",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(236, 72, 153))
            };
            Grid.SetColumn(tintTitle, 0);
            tintHeader.Children.Add(tintTitle);

            _lblTintVal = new TextBlock {
                Text = "0 (Neutral)",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = _textSecondary
            };
            Grid.SetColumn(_lblTintVal, 1);
            tintHeader.Children.Add(_lblTintVal);
            sp.Children.Add(tintHeader);

            Border tintSpectrum = new Border {
                Height = 6,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 0, 4),
                Background = new LinearGradientBrush(new GradientStopCollection {
                    new GradientStop(System.Windows.Media.Color.FromRgb(236, 72, 153), 0.0),
                    new GradientStop(System.Windows.Media.Color.FromRgb(156, 163, 175), 0.5),
                    new GradientStop(System.Windows.Media.Color.FromRgb(16, 185, 129), 1.0)
                }, new System.Windows.Point(0, 0), new System.Windows.Point(1, 0))
            };
            sp.Children.Add(tintSpectrum);

            _sliderTint = CreateStyledSlider(-50, 50, 0, 1);
            _sliderTint.ValueChanged += (s, e) => {
                int t = (int)_sliderTint.Value;
                if (t == 0) _lblTintVal.Text = "0 (Neutral)";
                else if (t < 0) _lblTintVal.Text = string.Format("{0} (Magenta Bias)", t);
                else _lblTintVal.Text = string.Format("+{0} (Green Bias)", t);
                TriggerSettingsChanged();
            };
            sp.Children.Add(_sliderTint);

            // Separator line
            Border sep = new Border {
                Height = 1,
                Background = _cardBorderBrush,
                Margin = new Thickness(0, 10, 0, 12)
            };
            sp.Children.Add(sep);

            // --- COLOR SATURATION & VIBRANCE ENGINE ---
            TextBlock satTitle = new TextBlock {
                Text = "COLOR SATURATION & VIBRANCE ENGINE",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(236, 72, 153)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            sp.Children.Add(satTitle);

            // Saturation (0.0 to 2.0, default 1.0)
            sp.Children.Add(CreateSliderRow(
                "Color Saturation & Richness",
                out _sliderSaturation,
                out _lblSaturationVal,
                0.0,
                2.0,
                1.0,
                0.01,
                System.Windows.Media.Color.FromRgb(236, 72, 153),
                val => val == 1.0 ? "100% (Neutral)" : (val < 1.0 ? string.Format("{0:0}% (Subtle)", val * 100) : string.Format("{0:0}% (Vivid Rich)", val * 100))
            ));

            // Vibrance (0.0 to 2.0, default 1.0)
            sp.Children.Add(CreateSliderRow(
                "Smart Midtone Vibrance",
                out _sliderVibrance,
                out _lblVibranceVal,
                0.0,
                2.0,
                1.0,
                0.01,
                System.Windows.Media.Color.FromRgb(244, 63, 94),
                val => val == 1.0 ? "100% (Neutral)" : (val < 1.0 ? string.Format("{0:0}% (Muted)", val * 100) : string.Format("{0:0}% (Punchy)", val * 100))
            ));

            // Separator line
            Border sep2 = new Border {
                Height = 1,
                Background = _cardBorderBrush,
                Margin = new Thickness(0, 10, 0, 12)
            };
            sp.Children.Add(sep2);

            // --- TOP NOTCH NEW WORLD SCREEN PROCESSING ---
            TextBlock engineTitle = new TextBlock {
                Text = "NEW WORLD SCREEN & NOISE REDUCTION ENGINE",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(168, 85, 247)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            sp.Children.Add(engineTitle);

            // 1. Clarity & Micro-Contrast Slider
            sp.Children.Add(CreateSliderRow(
                "Clarity & Micro-Contrast (Edge Pop)",
                out _sliderClarity,
                out _lblClarityVal,
                0.0,
                1.0,
                0.0,
                0.01,
                System.Windows.Media.Color.FromRgb(34, 211, 238),
                val => val == 0 ? "Off (0%)" : string.Format("{0:0}% (Ultra Pop)", val * 100)
            ));

            TextBlock clarityDesc = new TextBlock {
                Text = "S-curve perceptual dynamic tone mapping that brings out fine textures and razor-sharp clarity.",
                FontSize = 10,
                Foreground = _textMuted,
                Margin = new Thickness(0, -6, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            sp.Children.Add(clarityDesc);

            // 2. Noise Reduction Slider
            sp.Children.Add(CreateSliderRow(
                "Digital Noise Reduction & Black Floor Cleaner",
                out _sliderNoiseReduction,
                out _lblNoiseVal,
                0.0,
                1.0,
                0.0,
                0.01,
                System.Windows.Media.Color.FromRgb(168, 85, 247),
                val => val == 0 ? "Off (0%)" : string.Format("{0:0}% (OLED Deep Black)", val * 100)
            ));

            TextBlock noiseDesc = new TextBlock {
                Text = "Cleans shadow noise, cuts milky gray LCD backlight haze, and smooths HDMI gradient banding.",
                FontSize = 10,
                Foreground = _textMuted,
                Margin = new Thickness(0, -6, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            sp.Children.Add(noiseDesc);

            return sp;
        }

        private UIElement CreateKelvinChip(string text, int kelvin) {
            Button btn = new Button {
                Content = text,
                FontSize = 11,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 6, 6),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 36, 62)),
                Foreground = _textPrimary,
                BorderBrush = _cardBorderBrush,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            btn.Click += (s, e) => {
                _sliderKelvin.Value = kelvin;
            };
            return btn;
        }

        private void UpdateSwatchColor(int kelvin) {
            RgbMultiplier rgb = ColorEngine.GetRawKelvinRgb(kelvin);
            byte r = (byte)Math.Max(0, Math.Min(255, (int)rgb.R));
            byte g = (byte)Math.Max(0, Math.Min(255, (int)rgb.G));
            byte b = (byte)Math.Max(0, Math.Min(255, (int)rgb.B));
            _swatchKelvin.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
        }

        // ==========================================
        // RGB & DYNAMICS PANEL (RIGHT COLUMN)
        // ==========================================
        private UIElement BuildRgbAndDynamicsPanel() {
            StackPanel sp = new StackPanel();

            // Panel Title
            TextBlock title = new TextBlock {
                Text = "RGB COLOR BALANCE & CHANNELS",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(56, 189, 248)),
                Margin = new Thickness(0, 0, 0, 14)
            };
            sp.Children.Add(title);

            // Red Channel
            sp.Children.Add(CreateSliderRow("Red Channel Gain", out _sliderRed, out _lblRedVal, 0.0, 2.0, 1.0, 0.01, System.Windows.Media.Color.FromRgb(239, 68, 68), val => string.Format("{0:0}%", val * 100)));

            // Green Channel
            sp.Children.Add(CreateSliderRow("Green Channel Gain", out _sliderGreen, out _lblGreenVal, 0.0, 2.0, 1.0, 0.01, System.Windows.Media.Color.FromRgb(16, 185, 129), val => string.Format("{0:0}%", val * 100)));

            // Blue Channel
            sp.Children.Add(CreateSliderRow("Blue Channel Gain", out _sliderBlue, out _lblBlueVal, 0.0, 2.0, 1.0, 0.01, System.Windows.Media.Color.FromRgb(59, 130, 246), val => string.Format("{0:0}%", val * 100)));

            // Reset RGB button
            Button btnResetRgb = new Button {
                Content = "Reset RGB Channels to 100%",
                FontSize = 11,
                Foreground = _textSecondary,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, -4, 0, 12)
            };
            btnResetRgb.Click += (s, e) => {
                _sliderRed.Value = 1.0;
                _sliderGreen.Value = 1.0;
                _sliderBlue.Value = 1.0;
            };
            sp.Children.Add(btnResetRgb);

            // Separator
            Border sep = new Border {
                Height = 1,
                Background = _cardBorderBrush,
                Margin = new Thickness(0, 0, 0, 14)
            };
            sp.Children.Add(sep);

            TextBlock dynTitle = new TextBlock {
                Text = "LUMINANCE & DISPLAY DYNAMICS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            sp.Children.Add(dynTitle);

            // Brightness (0.1 to 1.5)
            sp.Children.Add(CreateSliderRow("Brightness / Night Dimmer", out _sliderBrightness, out _lblBrightnessVal, 0.1, 1.5, 1.0, 0.01, System.Windows.Media.Color.FromRgb(245, 158, 11), val => string.Format("{0:0}%", val * 100)));

            // Contrast (0.5 to 1.5)
            sp.Children.Add(CreateSliderRow("Contrast Enhancement", out _sliderContrast, out _lblContrastVal, 0.5, 1.5, 1.0, 0.01, System.Windows.Media.Color.FromRgb(168, 85, 247), val => string.Format("{0:0}%", val * 100)));

            // Gamma (0.5 to 2.5)
            sp.Children.Add(CreateSliderRow("Gamma Curve Response", out _sliderGamma, out _lblGammaVal, 0.5, 2.5, 1.0, 0.01, System.Windows.Media.Color.FromRgb(14, 165, 233), val => string.Format("{0:0.00}x", val)));

            // Separator
            Border sep2 = new Border {
                Height = 1,
                Background = _cardBorderBrush,
                Margin = new Thickness(0, 8, 0, 12)
            };
            sp.Children.Add(sep2);

            TextBlock shadowTitle = new TextBlock {
                Text = "SHADOW DETAIL & HIGHLIGHT TONING (TV FIX)",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 211, 153)),
                Margin = new Thickness(0, 0, 0, 8)
            };
            sp.Children.Add(shadowTitle);

            // Shadow Lift (-20 to +40, default 0)
            sp.Children.Add(CreateSliderRow(
                "Black Level / Shadow Lift (TV Fix)",
                out _sliderShadowLift,
                out _lblShadowLiftVal,
                -20.0,
                40.0,
                0.0,
                1.0,
                System.Windows.Media.Color.FromRgb(52, 211, 153),
                val => val == 0 ? "0 (Standard)" : (val > 0 ? string.Format("+{0:0} (Reveal Shadows)", val) : string.Format("{0:0} (Deep Blacks)", val))
            ));

            // Highlight Softening (0.0 to 1.0, default 0.0)
            sp.Children.Add(CreateSliderRow(
                "Highlight Softening & Anti-Glare",
                out _sliderHighlightRolloff,
                out _lblHighlightRolloffVal,
                0.0,
                1.0,
                0.0,
                0.01,
                System.Windows.Media.Color.FromRgb(251, 146, 60),
                val => val == 0.0 ? "Off (0%)" : string.Format("{0:0}% (Soft Roll-off)", val * 100)
            ));

            return sp;
        }

        private UIElement CreateSliderRow(
            string label,
            out Slider slider,
            out TextBlock lblVal,
            double min,
            double max,
            double defaultVal,
            double step,
            System.Windows.Media.Color accentColor,
            Func<double, string> formatVal)
        {
            StackPanel row = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            TextBlock title = new TextBlock {
                Text = label,
                FontSize = 12,
                Foreground = _textPrimary,
                FontWeight = FontWeights.Medium
            };
            Grid.SetColumn(title, 0);
            header.Children.Add(title);

            lblVal = new TextBlock {
                Text = formatVal(defaultVal),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(accentColor)
            };
            Grid.SetColumn(lblVal, 1);
            header.Children.Add(lblVal);
            row.Children.Add(header);

            slider = CreateStyledSlider(min, max, defaultVal, step);
            TextBlock capturedLbl = lblVal;
            slider.ValueChanged += (s, e) => {
                capturedLbl.Text = formatVal(e.NewValue);
                TriggerSettingsChanged();
            };
            row.Children.Add(slider);

            return row;
        }

        // ==========================================
        // PRESETS & CUSTOM PROFILES SECTION
        // ==========================================
        private UIElement BuildPresetsSection() {
            Border card = CreateCard();
            card.Margin = new Thickness(0, 16, 0, 0);

            StackPanel sp = new StackPanel();

            // Section Header
            StackPanel titlePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            TextBlock title = new TextBlock {
                Text = "TOP-NOTCH MASTER PICTURE MODES & PRESETS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(167, 139, 250)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            titlePanel.Children.Add(title);

            TextBlock sub = new TextBlock {
                Text = "Transform your external monitor into a cinema-grade, noise-free, high-definition screen",
                FontSize = 12,
                Foreground = _textSecondary
            };
            titlePanel.Children.Add(sub);
            sp.Children.Add(titlePanel);

            // --- PROMINENT INLINE CUSTOM PRESET MANAGEMENT BAR ---
            Border customPresetBar = new Border {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 32, 54)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(49, 65, 102)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 4, 0, 14)
            };

            Grid customGrid = new Grid();
            customGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            customGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) }); // Gap
            customGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left: Save Current Settings as Custom Preset
            StackPanel saveSide = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            TextBlock lblSave = new TextBlock {
                Text = "Save As:",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(167, 139, 250)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            saveSide.Children.Add(lblSave);

            _txtCustomPresetName = new TextBox {
                Width = 160,
                Height = 32,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 21, 37)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = _cardBorderBrush,
                Padding = new Thickness(8, 6, 8, 6),
                VerticalAlignment = VerticalAlignment.Center,
                Text = "My Custom Mode",
                Margin = new Thickness(0, 0, 8, 0)
            };
            saveSide.Children.Add(_txtCustomPresetName);

            Button btnSave = CreateActionButton("💾 Save Current", System.Windows.Media.Color.FromRgb(79, 70, 229));
            btnSave.Click += (s, e) => {
                string name = _txtCustomPresetName.Text.Trim();
                if (!string.IsNullOrEmpty(name)) {
                    DisplayProfile current = GetCurrentUIProfile();
                    current.ActivePresetName = name;
                    _settings.CustomPresets[name] = current;
                    SettingsManager.SaveSettings(_settings);
                    RefreshCustomPresetsCombo();
                    for (int i = 0; i < _cboCustomPresets.Items.Count; i++) {
                        if (_cboCustomPresets.Items[i].ToString() == name) {
                            _cboCustomPresets.SelectedIndex = i;
                            break;
                        }
                    }
                    _txtStatus.Text = string.Format("✓ Saved Custom Preset: '{0}'", name);
                }
            };
            saveSide.Children.Add(btnSave);
            Grid.SetColumn(saveSide, 0);
            customGrid.Children.Add(saveSide);

            // Right: Saved Presets Dropdown & Delete
            StackPanel loadSide = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            TextBlock lblSaved = new TextBlock {
                Text = "Library:",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = _textSecondary,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            loadSide.Children.Add(lblSaved);

            _cboCustomPresets = new ComboBox {
                Width = 160,
                Height = 32,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 21, 37)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = _cardBorderBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            ApplyDarkComboBoxStyle(_cboCustomPresets);
            RefreshCustomPresetsCombo();
            _cboCustomPresets.SelectionChanged += (s, e) => {
                if (_cboCustomPresets.SelectedIndex > 0 && _cboCustomPresets.SelectedItem != null) {
                    string presetName = _cboCustomPresets.SelectedItem.ToString();
                    if (_settings.CustomPresets.ContainsKey(presetName)) {
                        ApplyPresetProfile(_settings.CustomPresets[presetName], presetName);
                    }
                }
            };
            loadSide.Children.Add(_cboCustomPresets);

            Button btnDelete = CreateActionButton("🗑️ Delete", System.Windows.Media.Color.FromRgb(185, 28, 28));
            btnDelete.Click += (s, e) => {
                if (_cboCustomPresets.SelectedIndex > 0 && _cboCustomPresets.SelectedItem != null) {
                    string presetName = _cboCustomPresets.SelectedItem.ToString();
                    if (_settings.CustomPresets.ContainsKey(presetName)) {
                        _settings.CustomPresets.Remove(presetName);
                        SettingsManager.SaveSettings(_settings);
                        RefreshCustomPresetsCombo();
                        _txtStatus.Text = string.Format("✓ Deleted Custom Preset: '{0}'", presetName);
                    }
                }
            };
            loadSide.Children.Add(btnDelete);
            Grid.SetColumn(loadSide, 2);
            customGrid.Children.Add(loadSide);

            customPresetBar.Child = customGrid;
            sp.Children.Add(customPresetBar);

            // Presets Cards Grid
            _presetsPanel = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
            AddPresetCard("🔥 Bright Laptop Match (Zero Darkness)", "6200K • Bright Open Slate • High Luminance", 6200, 1.10, 1.05, 0.94, -2, 1.08, 1.05, 0.98, 0.0, 0.25, 1.18, 1.22, 6.0, 0.0);
            AddPresetCard("✨ Crisp Daylight Pop", "6400K • Clean White Taskbar • Punchy Colors", 6400, 1.06, 1.02, 0.92, -2, 1.06, 1.08, 0.99, 0.0, 0.35, 1.22, 1.25, 8.0, 0.0);
            AddPresetCard("🎯 Perfect Match (HP OMEN)", "6300K • Pure White Taskbar & Sky/Water", 6300, 1.04, 1.02, 0.90, -1, 1.04, 1.06, 1.00, 0.0, 0.30, 1.20, 1.22, 6.0, 0.05);
            AddPresetCard("✨ Ultra Color Pop (AMOLED)", "6500K • High Saturation & Dynamic Punch", 6500, 1.04, 1.00, 0.95, -2, 1.05, 1.12, 1.02, 0.0, 0.50, 1.35, 1.40, 6.0, 0.05);
            AddPresetCard("🌟 New World Master OLED", "5500K • Inky Blacks + Micro-Contrast", 5500, 1.0, 1.0, 0.95, -2, 1.0, 1.15, 1.0, 0.65, 0.50, 1.22, 1.25, 4.0, 0.15);
            AddPresetCard("💎 Crystal 4K Detail", "6500K • Razor Edge Pop + Denoise", 6500, 1.0, 1.0, 1.0, 0, 1.05, 1.10, 1.0, 0.30, 0.75, 1.18, 1.20, 6.0, 0.10);
            AddPresetCard("🎯 Match Laptop (Open Shadows)", "5400K • Warm Filmic • Open Slate Grays", 5400, 1.02, 1.00, 0.85, 2, 1.02, 1.04, 1.04, 0.0, 0.25, 1.15, 1.20, 14.0, 0.05);
            AddPresetCard("💻 Match Laptop (Deep Anti-Blue)", "5000K • Heavy Blue Cut (Eye Relief)", 5000, 1.04, 1.00, 0.72, 4, 1.02, 1.02, 1.06, 0.0, 0.20, 1.10, 1.15, 12.0, 0.0);
            AddPresetCard("🛡️ HDMI Clean & Anti-Band", "6500K • Eliminates Stream Noise", 6500, 1.0, 1.0, 1.0, 0, 1.0, 1.05, 1.0, 0.80, 0.25, 1.05, 1.10, 6.0, 0.0);
            AddPresetCard("🎬 Cinema Warm", "5400K • Filmic Warmth & Skin Tones", 5400, 1.0, 1.0, 0.92, -2, 1.0, 1.10, 1.02, 0.40, 0.30, 1.15, 1.20, 6.0, 0.20);
            AddPresetCard("🌙 Night Owl Relax", "2700K • Eye Care Blue Blocker", 2700, 1.0, 0.95, 0.70, 0, 0.90, 1.0, 1.0, 0.50, 0.20, 0.95, 0.95, 8.0, 0.35);
            AddPresetCard("🎮 Gaming & Vivid Pop", "6500K • Dynamic Punch & Saturation", 6500, 1.05, 1.0, 1.02, 0, 1.05, 1.15, 1.05, 0.20, 0.60, 1.30, 1.35, 5.0, 0.05);
            AddPresetCard("🖥️ sRGB Studio Neutral", "6500K • Reference Flat Calibration", 6500, 1.0, 1.0, 1.0, 0, 1.0, 1.0, 1.0, 0.0, 0.0, 1.00, 1.00, 0.0, 0.0);
            AddPresetCard("🌌 Deep Space Pitch Black", "2200K • 40% Brightness Zero Fog", 2200, 1.0, 0.90, 0.60, 0, 0.40, 0.95, 0.95, 0.85, 0.10, 0.90, 0.90, -5.0, 0.40);

            sp.Children.Add(_presetsPanel);

            card.Child = sp;
            return card;
        }

        private void AddPresetCard(
            string name,
            string desc,
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
            double saturation,
            double vibrance,
            double shadowLift,
            double highlightRolloff)
        {
            Border btn = new Border {
                Width = 240,
                Height = 68,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 36, 62)),
                BorderBrush = _cardBorderBrush,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 10, 10),
                Padding = new Thickness(12, 10, 12, 10),
                Cursor = Cursors.Hand
            };

            StackPanel sp = new StackPanel();
            TextBlock title = new TextBlock {
                Text = name,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = _textPrimary
            };
            TextBlock sub = new TextBlock {
                Text = desc,
                FontSize = 10,
                Foreground = _textSecondary,
                Margin = new Thickness(0, 2, 0, 0)
            };
            sp.Children.Add(title);
            sp.Children.Add(sub);
            btn.Child = sp;

            btn.MouseEnter += (s, e) => btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(36, 48, 80));
            btn.MouseLeave += (s, e) => btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 36, 62));
            btn.MouseDown += (s, e) => {
                DisplayProfile p = new DisplayProfile {
                    Kelvin = kelvin,
                    RedGain = rGain,
                    GreenGain = gGain,
                    BlueGain = bGain,
                    Tint = tint,
                    Brightness = brightness,
                    Contrast = contrast,
                    Gamma = gamma,
                    NoiseReduction = noiseReduction,
                    Clarity = clarity,
                    Saturation = saturation,
                    Vibrance = vibrance,
                    ShadowLift = shadowLift,
                    HighlightRolloff = highlightRolloff,
                    ActivePresetName = name
                };
                ApplyPresetProfile(p, name);
            };

            _presetsPanel.Children.Add(btn);
        }

        private void AddPresetCard(
            string name,
            string desc,
            int kelvin,
            double rGain,
            double gGain,
            double bGain,
            double tint,
            double brightness,
            double contrast,
            double gamma,
            double noiseReduction,
            double clarity)
        {
            AddPresetCard(name, desc, kelvin, rGain, gGain, bGain, tint, brightness, contrast, gamma, noiseReduction, clarity, 1.0, 1.0, 0.0, 0.0);
        }

        private void ApplyPresetProfile(DisplayProfile p, string name) {
            LoadProfileToUI(p);
            _txtStatus.Text = string.Format("✓ Activated Preset: '{0}' on {1}", name, _currentDisplay != null ? _currentDisplay.FriendlyName : "Display");
        }

        private void PromptSaveCustomPreset() {
            Window dlg = new Window {
                Title = "Save Custom Preset",
                Width = 360,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = _bgBrush,
                Foreground = _textPrimary,
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel sp = new StackPanel { Margin = new Thickness(20) };
            TextBlock lbl = new TextBlock { Text = "Enter a name for this custom preset:", Margin = new Thickness(0, 0, 0, 10) };
            TextBox txt = new TextBox {
                Height = 30,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 36, 62)),
                Foreground = _textPrimary,
                BorderBrush = _cardBorderBrush,
                Padding = new Thickness(6, 4, 6, 4),
                Text = _txtCustomPresetName != null ? _txtCustomPresetName.Text : "My Top-Notch Mode"
            };
            sp.Children.Add(lbl);
            sp.Children.Add(txt);

            StackPanel btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            Button btnOk = CreateActionButton("Save", System.Windows.Media.Color.FromRgb(79, 70, 229));
            btnOk.Click += (s, e) => {
                string name = txt.Text.Trim();
                if (!string.IsNullOrEmpty(name)) {
                    DisplayProfile current = GetCurrentUIProfile();
                    current.ActivePresetName = name;
                    _settings.CustomPresets[name] = current;
                    SettingsManager.SaveSettings(_settings);
                    RefreshCustomPresetsCombo();
                    _txtStatus.Text = string.Format("✓ Saved Custom Preset: '{0}'", name);
                    dlg.Close();
                }
            };
            Button btnCancel = CreateActionButton("Cancel", System.Windows.Media.Color.FromRgb(40, 50, 75));
            btnCancel.Margin = new Thickness(8, 0, 0, 0);
            btnCancel.Click += (s, e) => dlg.Close();

            btnRow.Children.Add(btnOk);
            btnRow.Children.Add(btnCancel);
            sp.Children.Add(btnRow);

            dlg.Content = sp;
            dlg.ShowDialog();
        }

        private void RefreshCustomPresetsCombo() {
            _cboCustomPresets.Items.Clear();
            _cboCustomPresets.Items.Add("-- Saved Presets Library --");
            foreach (var key in _settings.CustomPresets.Keys) {
                _cboCustomPresets.Items.Add(key);
            }
            _cboCustomPresets.SelectedIndex = 0;
        }

        // ==========================================
        // PROFILE APPLICATION & GDI HARDWARE ENGINE
        // ==========================================
        private void LoadProfileToUI(DisplayProfile p) {
            _sliderKelvin.Value = p.Kelvin;
            _sliderTint.Value = p.Tint;
            _sliderRed.Value = p.RedGain;
            _sliderGreen.Value = p.GreenGain;
            _sliderBlue.Value = p.BlueGain;
            _sliderBrightness.Value = p.Brightness;
            _sliderContrast.Value = p.Contrast;
            _sliderGamma.Value = p.Gamma;
            _sliderNoiseReduction.Value = p.NoiseReduction;
            _sliderClarity.Value = p.Clarity;
            if (_sliderSaturation != null) _sliderSaturation.Value = p.Saturation > 0 ? p.Saturation : 1.0;
            if (_sliderVibrance != null) _sliderVibrance.Value = p.Vibrance > 0 ? p.Vibrance : 1.0;
            if (_sliderShadowLift != null) _sliderShadowLift.Value = p.ShadowLift;
            if (_sliderHighlightRolloff != null) _sliderHighlightRolloff.Value = p.HighlightRolloff;

            _lblKelvinVal.Text = string.Format("{0} K", p.Kelvin);
            _lblKelvinDesc.Text = ColorEngine.GetKelvinDescription(p.Kelvin);
            UpdateSwatchColor(p.Kelvin);

            ApplyCurrentSettingsCore();
        }

        private DisplayProfile GetCurrentUIProfile() {
            return new DisplayProfile {
                Kelvin = (int)_sliderKelvin.Value,
                Tint = _sliderTint.Value,
                RedGain = _sliderRed.Value,
                GreenGain = _sliderGreen.Value,
                BlueGain = _sliderBlue.Value,
                Brightness = _sliderBrightness.Value,
                Contrast = _sliderContrast.Value,
                Gamma = _sliderGamma.Value,
                NoiseReduction = _sliderNoiseReduction.Value,
                Clarity = _sliderClarity.Value,
                Saturation = _sliderSaturation != null ? _sliderSaturation.Value : 1.0,
                Vibrance = _sliderVibrance != null ? _sliderVibrance.Value : 1.0,
                ShadowLift = _sliderShadowLift != null ? _sliderShadowLift.Value : 0.0,
                HighlightRolloff = _sliderHighlightRolloff != null ? _sliderHighlightRolloff.Value : 0.0
            };
        }

        private void TriggerSettingsChanged() {
            if (_isComparingOriginal) {
                _isComparingOriginal = false;
                _btnCompare.Content = "⚡ A/B Compare (Original)";
                _btnCompare.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59));
            }

            _pendingApply = true;
            if (!_applyThrottleTimer.IsEnabled) {
                _applyThrottleTimer.Start();
            }
        }

        private void ApplyCurrentSettingsCore() {
            if (_currentDisplay == null) return;

            DisplayProfile prof = GetCurrentUIProfile();

            // Save to settings object
            _settings.DisplayProfiles[_currentDisplay.DeviceName] = prof;
            SettingsManager.SaveSettings(_settings);

            // Build gamma ramp with all 14 parameters
            _activeCustomRamp = ColorEngine.BuildGammaRamp(
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

            if (_settings.SyncAllDisplays) {
                foreach (var d in _displayManager.Displays) {
                    _displayManager.ApplyRamp(d.DeviceName, _activeCustomRamp);
                }
            } else {
                _displayManager.ApplyRamp(_currentDisplay.DeviceName, _activeCustomRamp);
            }
        }

        private void ToggleCompareOriginal() {
            if (_currentDisplay == null) return;

            _isComparingOriginal = !_isComparingOriginal;
            if (_isComparingOriginal) {
                _btnCompare.Content = "● Viewing Original (Click to Restore)";
                _btnCompare.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));
                _displayManager.ResetDisplay(_currentDisplay.DeviceName);
                _txtStatus.Text = "Viewing uncalibrated factory display default";
            } else {
                _btnCompare.Content = "⚡ A/B Compare (Original)";
                _btnCompare.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59));
                if (_activeCustomRamp != null) {
                    _displayManager.ApplyRamp(_currentDisplay.DeviceName, _activeCustomRamp);
                }
                _txtStatus.Text = "Custom Top-Notch color processing restored";
            }
        }

        private void ResetCurrentDisplay() {
            if (_currentDisplay == null) return;

            DisplayProfile def = new DisplayProfile();
            LoadProfileToUI(def);
            _displayManager.ResetDisplay(_currentDisplay.DeviceName);
            _txtStatus.Text = string.Format("↺ Reset {0} to factory linear default", _currentDisplay.FriendlyName);
        }

        // ==========================================
        // UI HELPERS (Styles, Cards, Buttons)
        // ==========================================
        private Border CreateCard() {
            return new Border {
                CornerRadius = new CornerRadius(12),
                Background = _cardBgBrush,
                BorderBrush = _cardBorderBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(20),
                Effect = new DropShadowEffect {
                    BlurRadius = 15,
                    ShadowDepth = 2,
                    Opacity = 0.25,
                    Color = System.Windows.Media.Color.FromRgb(0, 0, 0)
                }
            };
        }

        private Button CreateActionButton(string text, System.Windows.Media.Color bgColor) {
            Button btn = new Button {
                Content = text,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Padding = new Thickness(14, 8, 14, 8),
                Background = new SolidColorBrush(bgColor),
                Foreground = _textPrimary,
                BorderBrush = _cardBorderBrush,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            return btn;
        }

        private Slider CreateStyledSlider(double min, double max, double val, double step) {
            Slider s = new Slider {
                Minimum = min,
                Maximum = max,
                Value = val,
                SmallChange = step,
                LargeChange = step * 5,
                IsSnapToTickEnabled = false,
                TickFrequency = step,
                Margin = new Thickness(0, 4, 0, 8),
                Cursor = Cursors.Hand
            };
            return s;
        }

        private void ApplyDarkComboBoxStyle(ComboBox cbo) {
            cbo.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 36, 62));
            cbo.Foreground = System.Windows.Media.Brushes.White;
            cbo.BorderBrush = _cardBorderBrush;

            // Override popup system brushes so the popup window is dark
            cbo.Resources[System.Windows.SystemColors.WindowBrushKey] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 32, 54));
            cbo.Resources[System.Windows.SystemColors.WindowTextBrushKey] = System.Windows.Media.Brushes.White;
            cbo.Resources[System.Windows.SystemColors.HighlightBrushKey] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(79, 70, 229));
            cbo.Resources[System.Windows.SystemColors.HighlightTextBrushKey] = System.Windows.Media.Brushes.White;

            // Custom ItemContainerStyle for ComboBoxItem with dark background, crisp white text, and rich hover/selected states
            Style itemStyle = new Style(typeof(ComboBoxItem));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 32, 54))));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 247, 250))));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.PaddingProperty, new Thickness(10, 7, 10, 7)));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            itemStyle.Setters.Add(new Setter(ComboBoxItem.CursorProperty, Cursors.Hand));

            // Mouse hover highlight
            Trigger highlightTrigger = new Trigger { Property = ComboBoxItem.IsHighlightedProperty, Value = true };
            highlightTrigger.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(49, 65, 102))));
            highlightTrigger.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, System.Windows.Media.Brushes.White));
            itemStyle.Triggers.Add(highlightTrigger);

            // Active item selected
            Trigger selectedTrigger = new Trigger { Property = ComboBoxItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(ComboBoxItem.BackgroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(79, 70, 229))));
            selectedTrigger.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, System.Windows.Media.Brushes.White));
            itemStyle.Triggers.Add(selectedTrigger);

            cbo.ItemContainerStyle = itemStyle;
        }
    }
}
