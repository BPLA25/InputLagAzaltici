using System.Drawing;
using System.Windows.Forms;

namespace InputLagAzaltici
{
    public partial class MainForm
    {
        Panel BuildMonitorPage()
        {
            var pg = MkPage();
            pg.AutoScroll = true;
            int y = 12;

            Section(pg, "CANLI DURUM", ref y);
            lblCpu = MkLabel(pg, "CPU: -", 24, y); lblCpu.Font = UiKit.FontMono10; y += 22;
            lblRam = MkLabel(pg, "RAM: -", 24, y); lblRam.Font = UiKit.FontMono10; y += 22;
            lblMem = MkLabel(pg, "Boş/Standby: -", 24, y); lblMem.Font = UiKit.FontMono10; y += 22;
            lblTimerRes = MkLabel(pg, "Timer: -", 24, y); lblTimerRes.Font = UiKit.FontMono10; y += 22;
            lblPing = MkLabel(pg, "Ping: -", 24, y); lblPing.Font = UiKit.FontMono10; y += 30;

            MkLabel(pg, "Ping hedefi:", 24, y + 4, UiKit.Muted);
            txtPingHost = MkText(pg, "8.8.8.8", 110, y, 150);
            var bPing = new FlatButton { Text = "Ping testi (10 paket)" };
            bPing.SetBounds(270, y - 1, 160, 26);
            bPing.Click += (s, e) => MonitorLogic.OnPingTest(this);
            pg.Controls.Add(bPing);
            y += 40;

            Section(pg, "İŞLEMLER", ref y);
            lvProc = MkList(pg, 24, y, 800, 220, new[] { "Ad", "PID", "RAM (MB)", "CPU %", "Öncelik" }, new[] { 260, 70, 100, 80, 180 });
            y += 226;
            var bR = new FlatButton { Text = "Yenile" };
            bR.SetBounds(24, y, 80, 28);
            bR.Click += (s, e) => MonitorLogic.RefreshProcs(this);
            pg.Controls.Add(bR);
            var bLow = new FlatButton { Text = "Önceliği düşür" };
            bLow.SetBounds(110, y, 120, 28);
            bLow.Click += (s, e) => MonitorLogic.SetSelectedProcPriority(this, System.Diagnostics.ProcessPriorityClass.BelowNormal);
            pg.Controls.Add(bLow);
            var bNorm = new FlatButton { Text = "Normal yap" };
            bNorm.SetBounds(236, y, 100, 28);
            bNorm.Click += (s, e) => MonitorLogic.SetSelectedProcPriority(this, System.Diagnostics.ProcessPriorityClass.Normal);
            pg.Controls.Add(bNorm);
            var bKill = new FlatButton { Text = "İşlemi kapat" };
            bKill.SetBounds(342, y, 110, 28);
            bKill.Click += (s, e) => MonitorLogic.OnKillProc(this);
            pg.Controls.Add(bKill);
            y += 44;

            Section(pg, "BAŞLANGIÇ PROGRAMLARI", ref y);
            lvStartup = MkList(pg, 24, y, 800, 150, new[] { "Ad", "Komut", "Kaynak", "Durum" }, new[] { 180, 340, 140, 90 });
            y += 156;
            var bSR = new FlatButton { Text = "Yenile" };
            bSR.SetBounds(24, y, 80, 28);
            bSR.Click += (s, e) => MonitorLogic.RefreshStartup(this);
            pg.Controls.Add(bSR);
            var bST = new FlatButton { Text = "Aç / Kapat" };
            bST.SetBounds(110, y, 110, 28);
            bST.Click += (s, e) => MonitorLogic.OnToggleStartup(this);
            pg.Controls.Add(bST);
            y += 44;
            return pg;
        }
    }
}