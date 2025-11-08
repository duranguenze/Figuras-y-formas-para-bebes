namespace Keyer
{
    public partial class Form1 : Form
    {
        private KeyerConfig _cfg = new();
        private Label _overlay = new(); // mostrará la combinación de salida
        private HashSet<Keys> _pressed = new();
        private string _configPath = Path.Combine(AppContext.BaseDirectory, "keyer.config.json");

        private readonly Random _rng = new();
        private Rectangle _rect;
        private Color _rectColor = Color.Empty;
        private bool _rectVisible = false;
        private readonly System.Windows.Forms.Timer _rectTimer = new() { Interval =600 };
        private HashSet<string> _exitTokens = new(StringComparer.OrdinalIgnoreCase);

        private PictureBox _imageBox = new();
        private readonly Dictionary<string, Image> _imageCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _remoteCacheMap = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _cacheDir = Path.Combine(AppContext.BaseDirectory, "assets", "cache");
        private static readonly System.Net.Http.HttpClient _http = new();

        public Form1()
        {
            InitializeComponent();
            Load += Form1_Load;
            FormClosing += Form1_FormClosing;
            KeyPreview = true;
            DoubleBuffered = true;
            Paint += Form1_Paint;
            Resize += Form1_Resize;
            Directory.CreateDirectory(_cacheDir);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            TryLoadConfig();
            ParseExitTokens();
            ApplyWindowMode();
            SetupOverlay();
            SetupImageBox();
            await PrefetchRemoteImagesAsync();
            KeyboardHook.Start(KeyboardFilter);
            TopMost = _cfg.kiosk.topMost;
            if (_cfg.kiosk.hideCursor) Cursor.Hide(); else Cursor.Show();
            BackColor = Color.Black; // fondo negro

            _rectTimer.Tick += (_, __) => { HideVisuals(); };
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            KeyboardHook.Stop();
            foreach (var kv in _imageCache) kv.Value.Dispose();
        }

        private void HideVisuals()
        {
            _rectVisible = false;
            _imageBox.Visible = false;
            _rectTimer.Stop();
            Invalidate();
        }

        private void SetupImageBox()
        {
            _imageBox.Visible = false;
            _imageBox.SizeMode = PictureBoxSizeMode.Zoom;
            _imageBox.BackColor = Color.Transparent;
            _imageBox.Dock = DockStyle.None; // posicionaremos manualmente
            Controls.Add(_imageBox);
            BringToFront();
            _overlay.BringToFront();
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
            // actualizar tokens y overlay si ya está creado
            ParseExitTokens();
            if (_overlay != null)
            {
                _overlay.Text = $"Salir: {_cfg.input.exitCombo}";
                PositionOverlayBottomLeft();
                Invalidate();
            }
        }

        private void ParseExitTokens()
        {
            _exitTokens.Clear();
            var raw = _cfg.input.exitCombo ?? string.Empty;
            foreach (var part in raw.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                _exitTokens.Add(part);
        }

        private void ApplyWindowMode()
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = _cfg.kiosk.startFullscreen ? FormWindowState.Maximized : FormWindowState.Normal;
            Bounds = Screen.PrimaryScreen!.Bounds;
        }

        private void SetupOverlay()
        {
            _overlay.AutoSize = true;
            _overlay.TextAlign = ContentAlignment.MiddleLeft;
            _overlay.Visible = true;
            _overlay.Font = new Font(FontFamily.GenericSansSerif,18, FontStyle.Bold);
            _overlay.ForeColor = Color.White;
            _overlay.BackColor = Color.Transparent;
            _overlay.Text = $"Salir: {_cfg.input.exitCombo}";
            _overlay.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            Controls.Add(_overlay);
            PositionOverlayBottomLeft();
        }

        private void PositionOverlayBottomLeft()
        {
            var margin =12;
            // Colocar en esquina inferior izquierda con margen
            var size = TextRenderer.MeasureText(_overlay.Text, _overlay.Font);
            _overlay.Location = new Point(margin, ClientSize.Height - size.Height - margin);
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            PositionOverlayBottomLeft();
            Invalidate();
        }

        private bool KeyboardFilter(KeyboardHook.LowLevelKeyEvent e)
        {
            if (e.Kind == KeyboardHook.KeyEventKind.KeyDown) _pressed.Add(e.Key); else _pressed.Remove(e.Key);

            if (e.Kind == KeyboardHook.KeyEventKind.KeyDown)
            {
                if (IsExitPressed()) { BeginInvoke(new Action(() => Close())); return true; }

                // Bloqueos (mejor esfuerzo)
                if (_cfg.kiosk.blockWindowsKey && (e.Key == Keys.LWin || e.Key == Keys.RWin)) return true;
                if (_cfg.kiosk.blockAltTab && e.Alt && e.Key == Keys.Tab) return true;
                if (_cfg.kiosk.blockAltF4 && e.Alt && e.Key == Keys.F4) return true;

                bool showedImage = TryShowImageForKey(e.Key);
                if (!showedImage) ShowRandomRectangle();

                if (_cfg.sound.beepOnKey)
                {
                    try { Console.Beep(_cfg.sound.beepFrequency, _cfg.sound.beepDurationMs); } catch { }
                }
            }

            if (_cfg.input.blockAllKeysToOS)
            {
                return true; // bloquear hacia el SO
            }

            return false;
        }

        private bool TryShowImageForKey(Keys key)
        {
            string keyName = key.ToString();
            if (_cfg.actions.keys.TryGetValue(keyName, out var action) && string.Equals(action.type, "showImage", StringComparison.OrdinalIgnoreCase))
            {
                var val = action.value ?? string.Empty;
                string? chosen = null;
                try
                {
                    if (LooksLikeHttpUrl(val))
                    {
                        if (_remoteCacheMap.TryGetValue(val, out var local) && File.Exists(local))
                            chosen = local;
                        // si no existe aún, prefetch está en curso; fallback
                    }
                    else if (Directory.Exists(val))
                    {
                        var files = Directory.GetFiles(val).Where(HasImageExtension).ToArray();
                        if (files.Length >0) chosen = files[_rng.Next(files.Length)];
                    }
                    else if (File.Exists(val))
                    {
                        chosen = val;
                    }
                    if (chosen != null)
                    {
                        var img = GetCachedImage(chosen);
                        // calcular tamaño: máximo60% de ancho/alto
                        int maxW = (int)(ClientSize.Width *0.6);
                        int maxH = (int)(ClientSize.Height *0.6);
                        // ratio
                        double rw = (double)img.Width / img.Height;
                        int targetW = img.Width;
                        int targetH = img.Height;
                        if (targetW > maxW)
                        { targetW = maxW; targetH = (int)(targetW / rw); }
                        if (targetH > maxH)
                        { targetH = maxH; targetW = (int)(targetH * rw); }
                        int x = _rng.Next(0, Math.Max(1, ClientSize.Width - targetW));
                        int y = _rng.Next(0, Math.Max(1, ClientSize.Height - targetH));
                        _imageBox.Bounds = new Rectangle(x, y, targetW, targetH);
                        _imageBox.Image = img;
                        _imageBox.Visible = true;
                        _rectVisible = false;
                        _rectTimer.Interval = Math.Max(100, _cfg.visual.overlayAutoHideMs);
                        _rectTimer.Stop();
                        _rectTimer.Start();
                        Invalidate();
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }

        private bool HasImageExtension(string f)
        {
            string ext = Path.GetExtension(f).ToLowerInvariant();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif"; }

        private Image GetCachedImage(string path)
        {
            if (_imageCache.TryGetValue(path, out var img)) return img;
            var loaded = Image.FromFile(path);
            _imageCache[path] = loaded;
            return loaded;
        }

        private static bool LooksLikeHttpUrl(string s)
        {
            return s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        private async Task PrefetchRemoteImagesAsync()
        {
            try
            {
                var pairs = _cfg.actions.keys
                    .Where(kv => string.Equals(kv.Value.type, "showImage", StringComparison.OrdinalIgnoreCase))
                    .Select(kv => kv.Value.value)
                    .Where(v => !string.IsNullOrWhiteSpace(v) && LooksLikeHttpUrl(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                foreach (var url in pairs)
                {
                    try
                    {
                        var local = await DownloadToCacheAsync(url);
                        if (!string.IsNullOrEmpty(local))
                            _remoteCacheMap[url] = local;
                    }
                    catch { /* continuar con las demás */ }
                }
            }
            catch { }
        }

        private async Task<string?> DownloadToCacheAsync(string url)
        {
            try
            {
                var uri = new Uri(url);
                string ext = Path.GetExtension(uri.LocalPath);
                if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
                string fileName = SanitizeFileName(Path.GetFileNameWithoutExtension(uri.LocalPath));
                if (string.IsNullOrEmpty(fileName)) fileName = Guid.NewGuid().ToString("N");
                string local = Path.Combine(_cacheDir, fileName + ext);
                if (File.Exists(local)) return local;
                using var resp = await _http.GetAsync(uri);
                resp.EnsureSuccessStatusCode();
                var bytes = await resp.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(local, bytes);
                return local;
            }
            catch { return null; }
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private bool IsExitPressed()
        {
            if (_exitTokens.Count ==0) return false;
            // Construir tokens actuales (orden independiente)
            var current = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Modificadores activos
            if (IsModifierDown(Keys.ControlKey)) current.Add("Ctrl");
            if (IsModifierDown(Keys.ShiftKey)) current.Add("Shift");
            if (IsModifierDown(Keys.Menu)) current.Add("Alt");
            // Añadir teclas no modificadoras actualmente presionadas
            foreach (var k in _pressed)
            {
                if (k is Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.LWin or Keys.RWin) continue;
                current.Add(k.ToString());
            }
            // Igualdad de conjuntos
            if (current.Count != _exitTokens.Count) return false;
            foreach (var t in _exitTokens)
                if (!current.Contains(t)) return false;
            return true;
        }

        private bool IsModifierDown(Keys key)
        {
            return (Control.ModifierKeys & key) == key;
        }

        private void ShowRandomRectangle()
        {
            var minSize =40;
            var maxW = Math.Max(minSize, ClientSize.Width /2);
            var maxH = Math.Max(minSize, ClientSize.Height /2);
            var w = _rng.Next(minSize, Math.Max(minSize +1, maxW));
            var h = _rng.Next(minSize, Math.Max(minSize +1, maxH));
            var x = _rng.Next(0, Math.Max(1, ClientSize.Width - w));
            var y = _rng.Next(0, Math.Max(1, ClientSize.Height - h));
            _rect = new Rectangle(x, y, w, h);
            _rectColor = Color.FromArgb(255, _rng.Next(20,236), _rng.Next(20,236), _rng.Next(20,236));
            _rectVisible = true;
            _imageBox.Visible = false;
            Invalidate();

            _rectTimer.Interval = Math.Max(100, _cfg.visual.overlayAutoHideMs);
            _rectTimer.Stop();
            _rectTimer.Start();
        }

        private void Form1_Paint(object? sender, PaintEventArgs e)
        {
            // El fondo negro ya lo pinta WinForms con BackColor
            if (_rectVisible)
            {
                using var b = new SolidBrush(_rectColor);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.FillRectangle(b, _rect);
                using var pen = new Pen(Color.FromArgb(220,0,0,0),3);
                e.Graphics.DrawRectangle(pen, _rect);
            }
        }
    }
}
