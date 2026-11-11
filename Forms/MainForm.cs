using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace InputLagAzaltici
{
    public partial class MainForm : Form
    {
        static readonly int MemStatusSize = Marshal.SizeOf(typeof(Native.MS));
        static readonly Icon CachedIcon = SystemIcons.Application;

        internal string backupFile, settingsFile;
        internal BackupStore backupStore;
        internal List<RegTweak> tweaks = new List<RegTweak>();
        internal List<Opt> opts = new List<Opt>();
        internal Dictionary<string, Opt> optMap = new Dictionary<string, Opt>();
        internal HashSet<string> applied = new HashSet<string>();
        internal HashSet<string> failedOpts = new HashSet<string>();
        internal Dictionary<string, CheckBox> misc = new Dictionary<string, CheckBox>();
        internal ToolTip tip = new ToolTip();

        internal bool startTray, busy, loading, exiting;
        internal int errCount = 0;
        internal bool timerOn = false;
        internal int curPage = 0;
        internal bool uiReady = false;
        
        internal int currentTheme = 0; 
        internal Color selectedAccentColor = Color.FromArgb(88, 166, 255); 
        internal Color customBgColor = Color.Empty; 

        internal Color[] ColorPalette = new Color[]
        {
            Color.FromArgb(88, 166, 255), Color.FromArgb(248, 81, 73), Color.FromArgb(63, 185, 80), 
            Color.FromArgb(163, 113, 247), Color.FromArgb(255, 140, 0), Color.FromArgb(255, 20, 147), 
            Color.FromArgb(0, 255, 255), Color.FromArgb(255, 215, 0), Color.FromArgb(139, 69, 19), 
            Color.FromArgb(220, 20, 60), Color.FromArgb(0, 128, 0), Color.FromArgb(75, 0, 130), 
            Color.FromArgb(255, 105, 180), Color.FromArgb(64, 224, 208), Color.FromArgb(240, 230, 140), 
            Color.FromArgb(0, 0, 128), Color.FromArgb(128, 0, 0), Color.FromArgb(255, 99, 71), 
            Color.FromArgb(32, 178, 170), Color.FromArgb(186, 85, 211), Color.FromArgb(255, 0, 255), 
            Color.FromArgb(124, 252, 0), Color.FromArgb(255, 165, 0), Color.FromArgb(105, 105, 105)
        };

        internal Panel sidebar, topBar, statusBar, content;
        internal SidebarButton[] sideButtons;
        internal Panel[] pages = new Panel[5];
        
        internal Label lblHeaderStatus, lblHeaderApplied;
        internal ProgressBar headerProgress;
        internal Label lblStatusRight, lblBuildLabel;
        internal FlatButton btnApply, btnRevert, btnRefresh;
        internal RichTextBox log;
        internal NotifyIcon tray;
        internal ContextMenuStrip trayMenu;
        
        internal ComboBox cmbProfile = new ComboBox();
        internal ComboBox cmbSched = new ComboBox();
        internal NumericUpDown nudPagefile = new NumericUpDown();
        internal NumericUpDown nudStandby = new NumericUpDown();
        internal DateTimePicker dtSched = new DateTimePicker();
        internal TextBox txtPingHost = new TextBox();
        
        internal ListView lvProc, lvStartup;
        internal Label lblCpu, lblRam, lblMem, lblTimerRes, lblPing, lblDns, lblSpeed;
        internal Label lblScore, lblScoreDesc;
        
        internal Label lblDashScore, lblDashScoreDesc, lblDashCpu, lblDashRam, lblDashPing;
        internal ListBox lstDashLogs;

        internal CheckBox chkAutoStart = new CheckBox();
        internal CheckBox chkCloseToTray = new CheckBox();
        internal CheckBox chkVerify = new CheckBox();
        internal CheckBox chkRestorePoint = new CheckBox();
        internal CheckBox chkAutoRollback = new CheckBox();
        internal CheckBox chkSched = new CheckBox();
        internal CheckBox chkStandbyAuto = new CheckBox();
        internal CheckBox chkGmLow = new CheckBox { Checked = true }; 

        internal System.Windows.Forms.Timer monitor = new System.Windows.Forms.Timer();
        internal System.Windows.Forms.Timer sysRefresh = new System.Windows.Forms.Timer();

        internal long prevIdle, prevKernel, prevUser;
        internal int monitorTicks = 0;
        internal Ping pinger = new Ping();
        internal bool pingBusy;
        internal int lastPing = -1;
        internal DateTime lastSchedDay = DateTime.MinValue;
        internal DateTime lastPurge = DateTime.MinValue;
        internal DateTime lastSysRefresh = DateTime.MinValue;

        internal long cachedFreeMb = 0, cachedStandbyMb = 0;
        internal DateTime cachedMemTime = DateTime.MinValue;
        internal double cachedCpu = 0;
        internal DateTime cachedCpuTime = DateTime.MinValue;

        internal string[] dnsNames = { "Cloudflare", "Google", "Quad9", "OpenDNS" };
        internal string[][] dnsIps = {
            new[] { "1.1.1.1", "1.0.0.1" }, new[] { "8.8.8.8", "8.8.4.4" },
            new[] { "9.9.9.9", "149.112.112.112" }, new[] { "208.67.222.222", "208.67.220.220" }
        };
        internal int bestDns = -1;

        public MainForm(bool startTrayMode)
        {
            startTray = startTrayMode;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            backupFile = Path.Combine(baseDir, "yedek.ini");
            settingsFile = Path.Combine(baseDir, "ayarlar.ini");
            backupStore = new BackupStore(backupFile);

            AutoScaleMode = AutoScaleMode.None;
            Font = UiKit.FontBase;
            Text = "Input Lag Azaltici - Pro Edition";
            ClientSize = new Size(1120, 720);
            MinimumSize = new Size(980, 620);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiKit.Bg;
            ForeColor = UiKit.Text;

            tweaks = SystemTweaks.InitTweaks();
            BuildSidebar();
            BuildTopBar();
            BuildStatusBar();
            BuildContent();

            monitor.Interval = 1000;
            monitor.Tick += MonitorTick;

            pinger.PingCompleted += (s, e) =>
            {
                pingBusy = false;
                if (!e.Cancelled && e.Error == null && e.Reply != null && e.Reply.Status == IPStatus.Success) lastPing = (int)e.Reply.RoundtripTime;
                else lastPing = -1;
            };

            BuildTray();
            int build = RegistryHelper.GetBuild();
            if (build > 0 && build < 10240) { DisableOpt("dvr"); DisableOpt("memcomp"); }

            SettingsStore.Load(this);
            monitor.Start();
            ShowPage(0, false);
            uiReady = true;

            Resize += (s, e) => { if (WindowState == FormWindowState.Minimized) Hide(); };
            FormClosing += OnFormClosing;
            Load += (s, e) => { if (startTray) BeginInvoke(new Action(() => Hide())); };
            Shown += (s, e) => BeginInvoke(new Action(() =>
            {
                try { MemoryTweaks.ApplyStandbyList(); } catch { }
                backupStore.Load();
                CheckAutoStartTask();
                RefreshStatuses();
                Log("Hazir. Ayarlari secip 'Uygula'ya bas.");
            }));
        }

        void BuildSidebar()
        {
            sidebar = new Panel { Dock = DockStyle.Left, Width = 200, BackColor = UiKit.Surface };
            var logo = new Panel { Dock = DockStyle.Top, Height = 90 };
            logo.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var f1 = new Font("Segoe UI Semibold", 13f)) TextRenderer.DrawText(e.Graphics, "⚡ InputLag", f1, new Point(18, 22), UiKit.Text);
                using (var f2 = new Font("Segoe UI", 8f)) TextRenderer.DrawText(e.Graphics, "PRO EDITION v4", f2, new Point(20, 46), UiKit.Accent);
                using (var p = new Pen(UiKit.Border, 1)) e.Graphics.DrawLine(p, 0, 70, logo.Width, 70);
            };
            sidebar.Controls.Add(logo);

            string[] icons = { "🏠", "⚙", "📊", "🛠", "💻" };
            string[] names = { "Panel", "Ayarlar", "İzleme", "Araçlar", "Sistem" };
            sideButtons = new SidebarButton[names.Length];
            int y = 100;
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i;
                var b = new SidebarButton { Icon = icons[i], Text = names[i] };
                b.SetBounds(0, y, sidebar.Width, 42);
                b.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                b.Activate += (s, e) => ShowPage(idx, true);
                sidebar.Controls.Add(b);
                sideButtons[i] = b;
                y += 46;
            }

            var quick = new Panel { Height = 130, Dock = DockStyle.Bottom, BackColor = UiKit.Surface };
            quick.Paint += (s, e) => { using (var p = new Pen(UiKit.Border)) e.Graphics.DrawLine(p, 10, 0, quick.Width - 10, 0); };

            btnApply = new FlatButton { Text = "✔  Uygula", BaseColor = UiKit.Accent, HoverColor = UiKit.AccentDark, TextColor = Color.White, Font = UiKit.FontSemibold };
            btnApply.SetBounds(12, 10, 176, 36);
            btnApply.Click += (s, e) => Applier.ApplySelected(this, true);
            quick.Controls.Add(btnApply);

            btnRevert = new FlatButton { Text = "↺  Geri Al" };
            btnRevert.SetBounds(12, 52, 176, 32);
            btnRevert.Click += (s, e) => Applier.RevertAll(this, true);
            quick.Controls.Add(btnRevert);

            btnRefresh = new FlatButton { Text = "🔄  Yenile" };
            btnRefresh.SetBounds(12, 90, 176, 28);
            btnRefresh.Click += (s, e) => { RefreshStatuses(); if (curPage == 4) RefreshSystemTabs(true); Log("Yenilendi."); };
            quick.Controls.Add(btnRefresh);

            sidebar.Controls.Add(quick);
            Controls.Add(sidebar);
        }

        void BuildTopBar()
        {
            topBar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = UiKit.Bg };
            topBar.Paint += (s, e) => { using (var p = new Pen(UiKit.Border)) e.Graphics.DrawLine(p, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1); };
            
            lblHeaderStatus = new Label { Text = "●  Hazır", ForeColor = UiKit.Ok, Font = UiKit.FontSemibold, AutoSize = true, Location = new Point(20, 20), BackColor = Color.Transparent };
            topBar.Controls.Add(lblHeaderStatus);
            
            lblHeaderApplied = new Label { Text = "Uygulanan: 0 ayar", ForeColor = UiKit.Muted, AutoSize = true, Location = new Point(200, 22), BackColor = Color.Transparent };
            topBar.Controls.Add(lblHeaderApplied);
            
            // ÇARK BUTONU (Net Görünür)
            var btnConfig = new FlatButton { 
                Text = "⚙️", 
                Font = new Font("Segoe UI Emoji", 14f), 
                Width = 44, 
                Height = 36, 
                BaseColor = UiKit.Surface2, 
                HoverColor = UiKit.Border,
                TextColor = UiKit.Text
            };
            btnConfig.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConfig.Location = new Point(topBar.Width - 64, 14);
            btnConfig.Click += (s, e) => ShowConfigDialog();
            topBar.Controls.Add(btnConfig);

            headerProgress = new ProgressBar { Style = ProgressBarStyle.Continuous, Visible = false };
            headerProgress.SetBounds(20, 52, 400, 4);
            topBar.Controls.Add(headerProgress);
            Controls.Add(topBar);
        }

        void ShowConfigDialog()
        {
            Form frm = new Form();
            frm.Text = "Uygulama Ayarları & Tema";
            frm.Size = new Size(600, 680);
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.FormBorderStyle = FormBorderStyle.FixedDialog;
            frm.MaximizeBox = false; frm.MinimizeBox = false;
            frm.BackColor = UiKit.Surface; frm.ForeColor = UiKit.Text;
            frm.Font = UiKit.FontBase;

            frm.FormClosed += (s, e) => { ApplyTheme(); };

            int y = 20;

            var l0 = new Label { Text = "TEMA VE RENK", ForeColor = UiKit.Accent, Font = UiKit.FontSemibold, Location = new Point(20, y), AutoSize = true, BackColor = Color.Transparent };
            frm.Controls.Add(l0); y += 30;

            Label lblTheme = new Label { Text = "Arka Plan Teması:", Location = new Point(20, y), AutoSize = true, ForeColor = UiKit.Text, BackColor = Color.Transparent };
            frm.Controls.Add(lblTheme);
            ComboBox cmbTheme = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(150, y), Width = 150, BackColor = UiKit.Surface2, ForeColor = UiKit.Text, FlatStyle = FlatStyle.Flat };
            cmbTheme.Items.AddRange(new[] { "Tam Siyah", "Koyu Gri", "Gece Mavisi", "Açık Tema" });
            cmbTheme.SelectedIndex = currentTheme;
            cmbTheme.SelectedIndexChanged += (s, e) => { currentTheme = cmbTheme.SelectedIndex; customBgColor = Color.Empty; ApplyTheme(); frm.BackColor = UiKit.Surface; frm.ForeColor = UiKit.Text; };
            frm.Controls.Add(cmbTheme);

            var btnCustomBg = new FlatButton { Text = "🎨", Location = new Point(310, y), Size = new Size(35, 25), BaseColor = UiKit.Surface2 };
            btnCustomBg.Click += (s, e) =>
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    cd.FullOpen = true;
                    cd.Color = UiKit.Bg;
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        customBgColor = cd.Color;
                        currentTheme = -1; 
                        ApplyTheme();
                        frm.BackColor = UiKit.Surface; frm.ForeColor = UiKit.Text;
                    }
                }
            };
            frm.Controls.Add(btnCustomBg);
            y += 40;

            Label lblAccent = new Label { Text = "Vurgu Rengi:", Location = new Point(20, y), AutoSize = true, ForeColor = UiKit.Text, BackColor = Color.Transparent };
            frm.Controls.Add(lblAccent); y += 25;

            var pnlColors = new Panel { Location = new Point(20, y), Size = new Size(530, 90), BackColor = Color.Transparent };
            int col = 0; int row = 0;
            for (int i = 0; i < ColorPalette.Length; i++)
            {
                int idx = i;
                var btnColor = new FlatButton { Width = 30, Height = 30, Location = new Point(col * 38, row * 38), BaseColor = ColorPalette[i], HoverColor = ColorPalette[i], Radius = 15 };
                if (ColorPalette[i] == selectedAccentColor) { btnColor.FlatAppearance.BorderSize = 2; btnColor.FlatAppearance.BorderColor = Color.White; }
                btnColor.Click += (s, e) => { selectedAccentColor = ColorPalette[idx]; ApplyTheme(); frm.Close(); ShowConfigDialog(); };
                pnlColors.Controls.Add(btnColor);
                col++; if (col > 13) { col = 0; row++; }
            }
            frm.Controls.Add(pnlColors);
            
            var btnCustomColor = new FlatButton { Text = "🎨 Özel Renk", Location = new Point(430, y + 30), Size = new Size(120, 30), BaseColor = UiKit.Surface2 };
            btnCustomColor.Click += (s, e) =>
            {
                using (ColorDialog cd = new ColorDialog())
                {
                    cd.FullOpen = true;
                    cd.Color = selectedAccentColor;
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        selectedAccentColor = cd.Color;
                        ApplyTheme();
                        frm.Close();
                        ShowConfigDialog();
                    }
                }
            };
            frm.Controls.Add(btnCustomColor);
            y += 110;

            var l1 = new Label { Text = "OTOMASYON", ForeColor = UiKit.Accent, Font = UiKit.FontSemibold, Location = new Point(20, y), AutoSize = true, BackColor = Color.Transparent };
            frm.Controls.Add(l1); y += 30;

            chkAutoStart = new CheckBox { Text = "Windows açılışında tepside başlat", Checked = false, Location = new Point(20, y), AutoSize = true, ForeColor = UiKit.Text, BackColor = Color.Transparent };
            chkAutoStart.Click += OnAutoStartClick; frm.Controls.Add(chkAutoStart); y += 25;

            chkCloseToTray = new CheckBox { Text = "Kapatma düğmesi tepsiye küçültsün", Checked = true, Location = new Point(20, y), AutoSize = true, ForeColor = UiKit.Text, BackColor = Color.Transparent };
            frm.Controls.Add(chkCloseToTray); y += 25;

            chkVerify = new CheckBox { Text = "30 dakikada bir ayarları kontrol et", Checked = true, Location = new Point(20, y), AutoSize = true, ForeColor = UiKit.Text, BackColor = Color.Transparent };
            frm.Controls.Add(chkVerify); y += 25;

            chkRestorePoint = new CheckBox { Text = "Uygula'dan önce geri yükleme noktası oluştur", Checked = false, Location = new Point(20, y), AutoSize = true, ForeColor = UiKit.Text, BackColor = Color.Transparent };
            frm.Controls.Add(chkRestorePoint); y += 25;

            chkAutoRollback = new CheckBox { Text = "Çok hata olursa (5+) otomatik geri al", Checked = false, Location = new Point(20, y), AutoSize = true, ForeColor = UiKit.Text, BackColor = Color.Transparent };
            frm.Controls.Add(chkAutoRollback); y += 30;

            var l2 = new Label { Text = "BELLEK YÖNETİMİ", ForeColor = UiKit.Accent, Font = UiKit.FontSemibold, Location = new Point(20, y), AutoSize = true, BackColor = Color.Transparent };
            frm.Controls.Add(l2); y += 30;

            chkStandbyAuto = new CheckBox { Text = "Boş RAM şu MB'ın altına inince temizle:", Checked = false, Location = new Point(20, y), AutoSize = true, ForeColor = UiKit.Text, BackColor = Color.Transparent };
            frm.Controls.Add(chkStandbyAuto);
            nudStandby = new NumericUpDown { Minimum = 256, Maximum = 8192, Value = 1024, Location = new Point(310, y), Width = 80, BackColor = UiKit.Surface2, ForeColor = UiKit.Text, BorderStyle = BorderStyle.FixedSingle };
            frm.Controls.Add(nudStandby); y += 30;

            var l3 = new Label { Text = "ZAMANLAYICI", ForeColor = UiKit.Accent, Font = UiKit.FontSemibold, Location = new Point(20, y), AutoSize = true, BackColor = Color.Transparent };
            frm.Controls.Add(l3); y += 30;

            chkSched = new CheckBox { Text = "Her gün şu saatte profil uygula:", Checked = false, Location = new Point(20, y), AutoSize = true, ForeColor = UiKit.Text, BackColor = Color.Transparent };
            frm.Controls.Add(chkSched);
            dtSched = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Location = new Point(230, y), Width = 70, Value = DateTime.Today.AddHours(20) };
            frm.Controls.Add(dtSched);
            cmbSched = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(310, y), Width = 110, BackColor = UiKit.Surface2, ForeColor = UiKit.Text, FlatStyle = FlatStyle.Flat };
            cmbSched.Items.AddRange(new[] { "Güvenli", "Dengeli", "Agresif" });
            cmbSched.SelectedIndex = 1; frm.Controls.Add(cmbSched); y += 40;

            var bEx = new FlatButton { Text = "Dışa Aktar", Bounds = new Rectangle(20, y, 110, 28), BaseColor = UiKit.Surface2 };
            bEx.Click += (s, e) => SettingsStore.OnExport(this); frm.Controls.Add(bEx);

            var bIm = new FlatButton { Text = "İçe Aktar", Bounds = new Rectangle(140, y, 110, 28), BaseColor = UiKit.Surface2 };
            bIm.Click += (s, e) => SettingsStore.OnImport(this); frm.Controls.Add(bIm);

            var bSL = new FlatButton { Text = "Log'u Kaydet", Bounds = new Rectangle(260, y, 110, 28), BaseColor = UiKit.Surface2 };
            bSL.Click += (s, e) => SettingsStore.OnSaveLog(this); frm.Controls.Add(bSL);

            var bClose = new FlatButton { Text = "Kapat", Bounds = new Rectangle(480, 590, 80, 32), BaseColor = UiKit.Accent, TextColor = Color.White };
            bClose.Click += (s, e) => frm.Close(); frm.Controls.Add(bClose);

            frm.ShowDialog(this);
        }

        void BuildStatusBar()
        {
            statusBar = new Panel { Dock = DockStyle.Bottom, Height = 26, BackColor = UiKit.Surface };
            statusBar.Paint += (s, e) => { using (var p = new Pen(UiKit.Border)) e.Graphics.DrawLine(p, 0, 0, statusBar.Width, 0); };
            lblStatusRight = new Label { Text = "Hazır • Yönetici olarak çalışıyor", ForeColor = UiKit.Muted, Font = UiKit.FontSmall, AutoSize = true, Location = new Point(10, 5), BackColor = Color.Transparent };
            statusBar.Controls.Add(lblStatusRight);
            lblBuildLabel = new Label { Text = "Windows Build " + RegistryHelper.GetBuild() + "  •  .NET " + Environment.Version, ForeColor = UiKit.Muted, Font = UiKit.FontSmall, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, BackColor = Color.Transparent };
            statusBar.Controls.Add(lblBuildLabel);
            statusBar.Resize += (s, e) => lblBuildLabel.Location = new Point(statusBar.Width - lblBuildLabel.Width - 10, 5);
            Controls.Add(statusBar);
        }

        void BuildContent()
        {
            content = new Panel { Dock = DockStyle.Fill, BackColor = UiKit.Bg, Padding = new Padding(16) };
            pages[0] = BuildDashboardPage();
            pages[1] = BuildSettingsPage();
            pages[2] = BuildMonitorPage();
            pages[3] = BuildToolsPage();
            pages[4] = BuildSystemPage();

            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].Dock = DockStyle.Fill;
                pages[i].Visible = (i == 0);
                content.Controls.Add(pages[i]);
            }
            Controls.Add(content);
            content.BringToFront();
        }

        internal Panel MkPage() { return new Panel { BackColor = UiKit.Bg, AutoScroll = false }; }
        internal Label MkLabel(Control parent, string text, int x, int y, Color? c = null, bool bold = false)
        {
            var l = new Label { Text = text, AutoSize = true, Location = new Point(x, y), ForeColor = c ?? UiKit.Text, Font = bold ? UiKit.FontBold : UiKit.FontBase, BackColor = Color.Transparent };
            parent.Controls.Add(l); return l;
        }
        internal CheckBox MkCheck(Control parent, string text, int x, int y, int w, string tooltip, bool chk)
        {
            var c = new CheckedBox { Text = text, Checked = chk };
            c.SetBounds(x, y, w, 24);
            if (tooltip != null) tip.SetToolTip(c, tooltip);
            parent.Controls.Add(c); return c;
        }
        internal CheckBox MkMisc(Control parent, string key, string text, int x, int y, int w, string tooltip, bool chk)
        { var c = MkCheck(parent, text, x, y, w, tooltip, chk); misc[key] = c; return c; }
        internal TextBox MkText(Control parent, string text, int x, int y, int w)
        { var t = new TextBox { Text = text, BorderStyle = BorderStyle.FixedSingle, BackColor = UiKit.Surface2, ForeColor = UiKit.Text }; t.SetBounds(x, y, w, 24); parent.Controls.Add(t); return t; }
        internal ComboBox MkCombo(Control parent, int x, int y, int w, string[] items, int sel)
        {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, BackColor = UiKit.Surface2, ForeColor = UiKit.Text, FlatStyle = FlatStyle.Flat };
            c.SetBounds(x, y, w, 24); c.Items.AddRange(items); if (sel >= 0 && sel < items.Length) c.SelectedIndex = sel; parent.Controls.Add(c); return c;
        }
        internal NumericUpDown MkNum(Control parent, int x, int y, int w, int min, int max, int val)
        { var n = new NumericUpDown { Minimum = min, Maximum = max, Value = val, BackColor = UiKit.Surface2, ForeColor = UiKit.Text, BorderStyle = BorderStyle.FixedSingle }; n.SetBounds(x, y, w, 24); parent.Controls.Add(n); return n; }
        internal ListView MkList(Control parent, int x, int y, int w, int h, string[] cols, int[] widths)
        {
            var lv = new ListView { View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = false, BorderStyle = BorderStyle.FixedSingle, BackColor = UiKit.Surface2, ForeColor = UiKit.Text };
            typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(lv, true, null);
            lv.SetBounds(x, y, w, h);
            for (int i = 0; i < cols.Length; i++) lv.Columns.Add(cols[i], widths[i]);
            parent.Controls.Add(lv); return lv;
        }
        internal Opt AddOpt(Control parent, string id, string shortName, string text, string tooltip, int level, Action apply, Func<bool> check, ref int y)
        {
            var o = new Opt { Id = id, Short = shortName, Text = text, Level = level, Apply = apply, Check = check };
            o.Dot = new Label { Text = "○", ForeColor = UiKit.Muted, AutoSize = true, Location = new Point(16, y + 4), BackColor = Color.Transparent };
            parent.Controls.Add(o.Dot);
            o.Box = MkCheck(parent, text, 38, y, 660, tooltip, true);
            opts.Add(o); optMap[id] = o; y += 27; return o;
        }
        internal void DisableOpt(string id)
        { Opt o; if (optMap.TryGetValue(id, out o)) { o.Applicable = false; o.Box.Checked = false; o.Box.Enabled = false; tip.SetToolTip(o.Box, "Bu Windows sürümünde desteklenmiyor."); } }
        internal void Section(Panel parent, string title, ref int y)
        {
            var l = new Label { Text = title, AutoSize = true, Location = new Point(16, y), ForeColor = UiKit.Accent, Font = UiKit.FontSemibold, BackColor = Color.Transparent };
            parent.Controls.Add(l); var line = new Panel { BackColor = UiKit.Border }; line.SetBounds(16, y + 20, 808, 1); parent.Controls.Add(line); y += 30;
        }

        internal void RefreshStatuses()
        {
            if (!uiReady) return;
            foreach (var o in opts)
            {
                if (failedOpts.Contains(o.Id))
                { o.Dot.Tag = "dotErr"; o.Dot.Text = "●"; o.Dot.ForeColor = UiKit.Err; continue; }

                if (o.Check == null) { o.Dot.Tag = "dot"; continue; }
                bool val; try { val = o.Check(); } catch { o.Dot.Tag = "dot"; continue; }
                
                if (val == o.LastState && o.Dot.Tag != null && o.Dot.Tag.ToString() != "dotErr") continue;
                
                o.LastState = val; o.Dot.Tag = val ? "dot1" : "dot0"; o.Dot.Text = "●";
                o.Dot.ForeColor = val ? UiKit.Ok : UiKit.Muted;
            }
            UpdateHeader();
        }
        
        internal void UpdateHeader()
        {
            if (lblHeaderApplied != null) { var nt = "Uygulanan: " + applied.Count + " ayar"; if (lblHeaderApplied.Text != nt) lblHeaderApplied.Text = nt; }
            if (lblStatusRight != null) { var nt = "Hazır • " + applied.Count + " ayar uygulanmış • " + (timerOn ? "timer aktif" : "timer pasif"); if (lblStatusRight.Text != nt) lblStatusRight.Text = nt; }
        }

        internal void Log(string s)
        {
            AppLogger.Info(s);
            if (lstDashLogs != null && !lstDashLogs.IsDisposed)
            {
                if (lstDashLogs.InvokeRequired) lstDashLogs.BeginInvoke(new Action(() => UpdateDashLog(s)));
                else UpdateDashLog(s);
            }
            if (log == null) return;
            if (log.InvokeRequired) { log.BeginInvoke(new Action<string>(Log), s); return; }
            
            if (log.TextLength > 50000) 
            { 
                log.Select(0, log.GetFirstCharIndexFromLine(100)); 
                log.SelectedText = ""; 
            }

            Color c = UiKit.Text;
            if (s.StartsWith("[OK]")) c = UiKit.Ok;
            else if (s.StartsWith("[HATA]")) c = UiKit.Err;
            else if (s.StartsWith("[UYARI]")) c = UiKit.Warn;
            log.SelectionStart = log.TextLength; log.SelectionLength = 0; log.SelectionColor = c;
            log.AppendText(s + "\r\n"); log.SelectionColor = log.ForeColor; log.ScrollToCaret();
        }
        private void UpdateDashLog(string s)
        {
            lstDashLogs.Items.Insert(0, "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + s);
            if (lstDashLogs.Items.Count > 30) lstDashLogs.Items.RemoveAt(30);
        }
        internal void Warn(string s) { Log("[UYARI] " + s); }
        internal void SetStatus(string text, Color c)
        {
            if (lblHeaderStatus == null) return;
            if (lblHeaderStatus.InvokeRequired) { lblHeaderStatus.BeginInvoke(new Action(() => SetStatus(text, c))); return; }
            var newText = "●  " + text;
            if (lblHeaderStatus.Text == newText && lblHeaderStatus.ForeColor == c) return;
            lblHeaderStatus.Text = newText; lblHeaderStatus.ForeColor = c;
        }
        internal void SetBusy(bool b)
        {
            if (btnApply != null) btnApply.Enabled = !b;
            if (btnRevert != null) btnRevert.Enabled = !b;
            if (btnRefresh != null) btnRefresh.Enabled = !b;
        }
        internal bool Do(string name, Action a)
        {
            try { a(); Log("[OK] " + name); return true; }
            catch (Exception ex) { errCount++; Log("[HATA] " + name + ": " + ex.Message); AppLogger.Error(name, ex); return false; }
        }
        internal void ShowPage(int i, bool refresh)
        {
            curPage = i;
            for (int k = 0; k < pages.Length; k++) if (pages[k].Visible != (k == i)) pages[k].Visible = (k == i);
            for (int k = 0; k < sideButtons.Length; k++) if (sideButtons[k].Selected != (k == i)) { sideButtons[k].Selected = (k == i); sideButtons[k].Invalidate(); }
            if (!refresh) return;
            if (i == 0) HealthScore.Compute(this);
            else if (i == 1) RefreshStatuses();
            else if (i == 2) { MonitorLogic.RefreshProcs(this); MonitorLogic.RefreshStartup(this); }
            else if (i == 4) RefreshSystemTabs(true);
        }

        internal void ApplyTheme()
        {
            if (customBgColor != Color.Empty) 
            {
                UiKit.Bg = customBgColor;
                UiKit.Surface = ControlPaint.Light(customBgColor, 0.1f);
                UiKit.Surface2 = ControlPaint.Light(customBgColor, 0.2f);
                UiKit.Border = ControlPaint.Light(customBgColor, 0.3f);
                
                int brightness = (int)((customBgColor.R * 0.299) + (customBgColor.G * 0.587) + (customBgColor.B * 0.114));
                UiKit.Text = brightness > 128 ? Color.Black : Color.White;
                UiKit.Muted = brightness > 128 ? Color.DimGray : Color.LightGray;
            }
            else 
            {
                if (currentTheme == 0) 
                {
                    UiKit.Bg = Color.FromArgb(10, 10, 12);
                    UiKit.Surface = Color.FromArgb(18, 18, 22);
                    UiKit.Surface2 = Color.FromArgb(28, 28, 34);
                    UiKit.Border = Color.FromArgb(45, 45, 55);
                    UiKit.Text = Color.FromArgb(245, 245, 250);
                    UiKit.Muted = Color.FromArgb(140, 140, 155);
                }
                else if (currentTheme == 1) 
                {
                    UiKit.Bg = Color.FromArgb(30, 30, 35);
                    UiKit.Surface = Color.FromArgb(45, 45, 50);
                    UiKit.Surface2 = Color.FromArgb(60, 60, 65);
                    UiKit.Border = Color.FromArgb(75, 75, 80);
                    UiKit.Text = Color.FromArgb(245, 245, 250);
                    UiKit.Muted = Color.FromArgb(150, 150, 160);
                }
                else if (currentTheme == 2) 
                {
                    UiKit.Bg = Color.FromArgb(15, 23, 42);
                    UiKit.Surface = Color.FromArgb(30, 41, 59);
                    UiKit.Surface2 = Color.FromArgb(51, 65, 85);
                    UiKit.Border = Color.FromArgb(71, 85, 105);
                    UiKit.Text = Color.FromArgb(241, 245, 249);
                    UiKit.Muted = Color.FromArgb(148, 163, 184);
                }
                else 
                {
                    UiKit.Bg = Color.FromArgb(240, 240, 245);
                    UiKit.Surface = Color.White;
                    UiKit.Surface2 = Color.FromArgb(225, 225, 235);
                    UiKit.Border = Color.FromArgb(200, 200, 210);
                    UiKit.Text = Color.FromArgb(30, 30, 40);
                    UiKit.Muted = Color.FromArgb(100, 100, 115);
                }
            }

            UiKit.Accent = selectedAccentColor;
            UiKit.AccentDark = Color.FromArgb(Math.Max(0, (int)(selectedAccentColor.R * 0.7)), Math.Max(0, (int)(selectedAccentColor.G * 0.7)), Math.Max(0, (int)(selectedAccentColor.B * 0.7)));

            if (UiKit.Text == Color.Black || currentTheme == 3) btnApply.TextColor = Color.Black;
            else btnApply.TextColor = Color.White;

            BackColor = UiKit.Bg; ForeColor = UiKit.Text;
            if (sidebar != null) sidebar.BackColor = UiKit.Surface;
            if (topBar != null) topBar.BackColor = UiKit.Bg;
            if (statusBar != null) statusBar.BackColor = UiKit.Surface;
            if (content != null) content.BackColor = UiKit.Bg;

            if (btnApply != null) { btnApply.BaseColor = UiKit.Accent; btnApply.HoverColor = UiKit.AccentDark; btnApply.Invalidate(); }

            Recolor(this); 
            foreach (var b in sideButtons) b.Invalidate(); 
            foreach (var p in pages) p.Invalidate();
            Invalidate(true);
        }

        void Recolor(Control c)
        {
            if (c is Card) c.Invalidate();
            else if (c is FlatButton || c is SidebarButton || c is CheckedBox || c is TopTabButton) c.Invalidate();
            else if (c is Label) c.ForeColor = UiKit.Text;
            else if (c is RichTextBox) { c.BackColor = UiKit.Surface; c.ForeColor = UiKit.Text; }
            else if (c is TextBox) { c.BackColor = UiKit.Surface2; c.ForeColor = UiKit.Text; }
            else if (c is ListView) { c.BackColor = UiKit.Surface2; c.ForeColor = UiKit.Text; }
            else if (c is ComboBox) { c.BackColor = UiKit.Surface2; c.ForeColor = UiKit.Text; }
            else if (c is ListBox) { c.BackColor = UiKit.Surface2; c.ForeColor = UiKit.Text; }
            foreach (Control ch in c.Controls) Recolor(ch);
        }

        void BuildTray()
        {
            trayMenu = new ContextMenuStrip();
            var miShow = new ToolStripMenuItem("Göster"); miShow.Click += (s, e) => { Show(); WindowState = FormWindowState.Normal; Activate(); };
            var miApply = new ToolStripMenuItem("Seçili ayarları uygula"); miApply.Click += (s, e) => Applier.ApplySelected(this, false);
            var miRevert = new ToolStripMenuItem("Geri Al"); miRevert.Click += (s, e) => Applier.RevertAll(this, false);
            var miExit = new ToolStripMenuItem("Çıkış"); miExit.Click += (s, e) => { exiting = true; Close(); };
            trayMenu.Items.Add(miShow); trayMenu.Items.Add(new ToolStripSeparator()); trayMenu.Items.Add(miApply); trayMenu.Items.Add(miRevert); trayMenu.Items.Add(new ToolStripSeparator()); trayMenu.Items.Add(miExit);
            tray = new NotifyIcon { Icon = CachedIcon, Text = "Input Lag Azaltici", ContextMenuStrip = trayMenu, Visible = true };
            tray.DoubleClick += (s, e) => { Show(); WindowState = FormWindowState.Normal; Activate(); };
        }
        void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && chkCloseToTray != null && chkCloseToTray.Checked && !exiting) { e.Cancel = true; Hide(); return; }
            try { SetTimer(false); Native.SetThreadExecutionState(0x80000000); SettingsStore.Save(this); if (tray != null) tray.Visible = false; } catch { }
        }
        internal void SetTimer(bool on)
        {
            uint min, max, cur; Native.NtQueryTimerResolution(out min, out max, out cur);
            if (on) { Native.NtSetTimerResolution(max, true, out cur); Log("Timer: " + (cur / 10000.0).ToString("0.000") + " ms"); timerOn = true; }
            else if (timerOn) { Native.NtSetTimerResolution(max, false, out cur); timerOn = false; }
            UpdateHeader();
        }
        void OnAutoStartClick(object sender, EventArgs e) { AutoStartTask.SetAutoStart(this); }
        internal void CheckAutoStartTask() { AutoStartTask.Check(this); }
        internal void MonitorTick(object sender, EventArgs e) { MonitorLogic.Tick(this); }
    }
}