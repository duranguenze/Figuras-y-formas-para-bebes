namespace Keyer
{
    public partial class Form1 : Form
    {
        private KeyerConfig _cfg = new();
        private Label _overlay = new();
        private HashSet<Keys> _pressed = new();
        private string _configPath = Path.Combine(AppContext.BaseDirectory, "keyer.config.json");

        public Form1()
        {
            InitializeComponent();
            Load += Form1_Load;
            FormClosing += Form1_FormClosing;
            KeyPreview = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            TryLoadConfig();
            ApplyWindowMode();
            SetupOverlay();
            KeyboardHook.Start(KeyboardFilter);
            TopMost = _cfg.kiosk.topMost;
            if (_cfg.kiosk.hideCursor) Cursor.Hide(); else Cursor.Show();
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            KeyboardHook.Stop();
        }

        private void TryLoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                    _cfg = KeyerConfig.Load(_configPath);
                else
                    File.WriteAllText(_configPath, "{}");
            }
            catch
            {
                _cfg = new KeyerConfig();
            }
        }

        private void ApplyWindowMode()
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = _cfg.kiosk.startFullscreen ? FormWindowState.Maximized : FormWindowState.Normal;
            Bounds = Screen.PrimaryScreen!.Bounds;
        }

        private void SetupOverlay()
        {
            _overlay.AutoSize = false;
            _overlay.Dock = DockStyle.Fill;
            _overlay.TextAlign = ContentAlignment.MiddleCenter;
            _overlay.Visible = _cfg.visual.showKeyOverlay;
            _overlay.Font = new Font(FontFamily.GenericSansSerif, _cfg.visual.overlayFontSize, FontStyle.Bold);
            _overlay.ForeColor = ColorTranslator.FromHtml(_cfg.visual.overlayTextColor);
            var back = ColorTranslator.FromHtml(_cfg.visual.overlayBackColor);
            _overlay.BackColor = Color.FromArgb((int)(_cfg.visual.overlayBackOpacity * 255), back);
            Controls.Add(_overlay);
        }

        private System.Windows.Forms.Timer _hideTimer = new() { Interval = 600 };

        private bool KeyboardFilter(KeyboardHook.LowLevelKeyEvent e)
        {
            // Track current pressed keys
            if (e.Kind == KeyboardHook.KeyEventKind.KeyDown) _pressed.Add(e.Key); else _pressed.Remove(e.Key);

            // Build current modifiers + key string for exit combo
            var combo = BuildComboString(e);

            if (e.Kind == KeyboardHook.KeyEventKind.KeyDown)
            {
                if (IsExitCombo(combo))
                {
                    BeginInvoke(new Action(() => Close()));
                    return true; // swallow
                }

                // Block Windows key, Alt+Tab, Alt+F4 per config (best-effort)
                if (_cfg.kiosk.blockWindowsKey && (e.Key == Keys.LWin || e.Key == Keys.RWin)) return true;
                if (_cfg.kiosk.blockAltTab && e.Alt && e.Key == Keys.Tab) return true;
                if (_cfg.kiosk.blockAltF4 && e.Alt && e.Key == Keys.F4) return true;

                // Show overlay text and beep
                if (_cfg.visual.showKeyOverlay)
                {
                    ShowOverlayText(combo);
                }
                if (_cfg.sound.beepOnKey)
                {
                    try { Console.Beep(_cfg.sound.beepFrequency, _cfg.sound.beepDurationMs); } catch { }
                }
            }

            // If configured, block all keys from reaching OS
            if (_cfg.input.blockAllKeysToOS)
            {
                // Allow only our exit combo to reach Close handler; but still suppress to OS
                return true;
            }

            return false; // let OS handle
        }

        private string BuildComboString(KeyboardHook.LowLevelKeyEvent e)
        {
            var parts = new List<string>();
            if (e.Ctrl) parts.Add("Ctrl");
            if (e.Alt) parts.Add("Alt");
            if (e.Shift) parts.Add("Shift");
            if (e.Key != Keys.Menu && e.Key != Keys.ShiftKey && e.Key != Keys.ControlKey && e.Key != Keys.LWin && e.Key != Keys.RWin)
                parts.Add(e.Key.ToString());
            return string.Join("+", parts);
        }

        private bool IsExitCombo(string combo)
        {
            // Note: Ctrl+Alt+Del cannot be intercepted by user apps; it will always be handled by the OS.
            return string.Equals(combo, _cfg.input.exitCombo, StringComparison.OrdinalIgnoreCase);
        }

        private void ShowOverlayText(string text)
        {
            _overlay.Text = text;
            _overlay.Visible = true;
            _hideTimer.Stop();
            _hideTimer.Interval = Math.Max(100, _cfg.visual.overlayAutoHideMs);
            _hideTimer.Tick -= HideTimer_Tick;
            _hideTimer.Tick += HideTimer_Tick;
            _hideTimer.Start();
        }

        private void HideTimer_Tick(object? sender, EventArgs e)
        {
            _overlay.Visible = false;
            _hideTimer.Stop();
        }
    }
}
