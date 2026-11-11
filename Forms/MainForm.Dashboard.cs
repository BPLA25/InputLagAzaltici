using System;
using System.Drawing;
using System.Windows.Forms;

namespace InputLagAzaltici
{
    public partial class MainForm
    {
        Panel BuildDashboardPage()
        {
            var pg = new Panel { BackColor = UiKit.Bg, AutoScroll = true };
            
            var lblWelcome = new Label { Text = "Sistem Kontrol Paneli", Font = new Font("Segoe UI Semibold", 20f), ForeColor = UiKit.Text, AutoSize = true, Location = new Point(24, 20) };
            var lblSub = new Label { Text = "Gecikmeyi azaltmak için hazır.", Font = UiKit.FontBase, ForeColor = UiKit.Muted, AutoSize = true, Location = new Point(26, 55) };
            pg.Controls.Add(lblWelcome); pg.Controls.Add(lblSub);

            int cardY = 100;
            
            var cardHealth = new Card { Title = "SİSTEM SAĞLIĞI", Bounds = new Rectangle(24, cardY, 260, 160), CardColor = UiKit.Surface };
            lblDashScore = new Label { Text = "—", Font = new Font("Segoe UI Semibold", 48f), ForeColor = UiKit.Ok, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Bounds = new Rectangle(0, 50, 260, 80) };
            lblDashScoreDesc = new Label { Text = "Hesaplanıyor...", Font = UiKit.FontBase, ForeColor = UiKit.Muted, AutoSize = false, TextAlign = ContentAlignment.TopCenter, Bounds = new Rectangle(10, 130, 240, 20) };
            cardHealth.Controls.Add(lblDashScore); cardHealth.Controls.Add(lblDashScoreDesc);
            pg.Controls.Add(cardHealth);

            var cardLive = new Card { Title = "CANLI DURUM", Bounds = new Rectangle(300, cardY, 400, 160), CardColor = UiKit.Surface };
            lblDashCpu = new Label { Text = "CPU: -", Font = UiKit.FontMono10, ForeColor = UiKit.Text, AutoSize = true, Location = new Point(20, 50) };
            lblDashRam = new Label { Text = "RAM: -", Font = UiKit.FontMono10, ForeColor = UiKit.Text, AutoSize = true, Location = new Point(20, 75) };
            lblDashPing = new Label { Text = "Ping: -", Font = UiKit.FontMono10, ForeColor = UiKit.Text, AutoSize = true, Location = new Point(20, 100) };
            cardLive.Controls.Add(lblDashCpu); cardLive.Controls.Add(lblDashRam); cardLive.Controls.Add(lblDashPing);
            pg.Controls.Add(cardLive);

            int actionY = 290;
            var lblActions = new Label { Text = "HIZLI AKSİYONLAR", Font = UiKit.FontSemibold, ForeColor = UiKit.Accent, AutoSize = true, Location = new Point(24, actionY) };
            pg.Controls.Add(lblActions);

            int btnY = actionY + 30;
            var bApplyAll = new FlatButton { Text = "⚡ Tümünü Uygula", BaseColor = UiKit.Accent, HoverColor = UiKit.AccentDark, TextColor = Color.White, Font = UiKit.FontSemibold };
            bApplyAll.SetBounds(24, btnY, 160, 40);
            bApplyAll.Click += (s, e) => Applier.ApplySelected(this, true); // ApplySelected düzeltildi
            pg.Controls.Add(bApplyAll);

            var bSpeedTest = new FlatButton { Text = "🚀 Hız Testi" };
            bSpeedTest.SetBounds(194, btnY, 120, 40); bSpeedTest.Click += (s, e) => SpeedTest.OnSpeedTest(this); pg.Controls.Add(bSpeedTest);
            
            var bPingTest = new FlatButton { Text = "📡 Ping Testi" };
            bPingTest.SetBounds(324, btnY, 120, 40); bPingTest.Click += (s, e) => MonitorLogic.OnPingTest(this); pg.Controls.Add(bPingTest);

            int logY = btnY + 70;
            var lblRecent = new Label { Text = "SON İŞLEMLER", Font = UiKit.FontSemibold, ForeColor = UiKit.Accent, AutoSize = true, Location = new Point(24, logY) };
            pg.Controls.Add(lblRecent);

            lstDashLogs = new ListBox { Bounds = new Rectangle(24, logY + 30, 676, 150), BackColor = UiKit.Surface2, ForeColor = UiKit.Text, BorderStyle = BorderStyle.None, Font = UiKit.FontMono };
            pg.Controls.Add(lstDashLogs);

            return pg;
        }
    }
}