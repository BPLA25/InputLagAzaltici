using System;
using System.Windows.Forms;

namespace InputLagAzaltici
{
    public partial class MainForm
    {
        Panel BuildToolsPage()
        {
            var pg = MkPage();
            pg.AutoScroll = true;
            int y = 12;

            Section(pg, "İNTERNET HIZ TESTİ", ref y);
            var bSpeed = new FlatButton { Text = "⚡ Hız testi başlat", BaseColor = UiKit.Accent, HoverColor = UiKit.AccentDark, TextColor = System.Drawing.Color.White };
            bSpeed.SetBounds(24, y, 180, 30);
            bSpeed.Click += (s, e) => SpeedTest.OnSpeedTest(this);
            pg.Controls.Add(bSpeed);
            y += 38;
            lblSpeed = MkLabel(pg, "İndirme: -   Yükleme: -   Ping: -", 24, y);
            lblSpeed.Font = UiKit.FontMono10;
            y += 34;

            Section(pg, "DNS", ref y);
            var bT = new FlatButton { Text = "DNS ping testi" };
            bT.SetBounds(24, y, 130, 28);
            bT.Click += (s, e) => DnsTools.OnDnsTest(this);
            pg.Controls.Add(bT);
            var bA = new FlatButton { Text = "En hızlıyı uygula" };
            bA.SetBounds(160, y, 140, 28);
            bA.Click += (s, e) => DnsTools.OnDnsApply(this);
            pg.Controls.Add(bA);
            var bR = new FlatButton { Text = "DNS'i eski haline getir" };
            bR.SetBounds(306, y, 170, 28);
            bR.Click += (s, e) => DnsTools.OnDnsRestore(this);
            pg.Controls.Add(bR);
            y += 34;
            lblDns = MkLabel(pg, "Önce 'DNS ping testi'ni çalıştır.", 24, y, UiKit.Muted);
            y += 30;

            Section(pg, "LOG", ref y);
            log = new RichTextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = UiKit.FontMono,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                DetectUrls = false,
                BackColor = UiKit.Surface,
                ForeColor = UiKit.Text
            };
            log.SetBounds(24, y, 800, 260);
            pg.Controls.Add(log);
            y += 270;
            return pg;
        }
    }
}