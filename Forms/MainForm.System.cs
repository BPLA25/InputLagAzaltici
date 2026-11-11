using System;
using System.Drawing;
using System.Windows.Forms;

namespace InputLagAzaltici
{
    public partial class MainForm
    {
        Panel pnlSysContent;
        TopTabButton[] sysTabBtns;
        string currentSysTab = "rtbOverview";

        Panel BuildSystemPage()
        {
            var pg = new Panel { BackColor = UiKit.Bg };

            var pnlTabs = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UiKit.Bg, Padding = new Padding(24, 15, 24, 0) };
            string[] tabs = { "Genel", "CPU", "RAM", "GPU", "Disk", "Ağ", "Güç" };
            string[] tags = { "rtbOverview", "rtbCPU", "rtbRAM", "rtbGPU", "rtbDisk", "rtbNet", "rtbPower" };
            
            sysTabBtns = new TopTabButton[tabs.Length];
            int x = 0;
            for (int i = 0; i < tabs.Length; i++)
            {
                int idx = i;
                var btn = new TopTabButton { Text = tabs[i], Tag = tags[i], Width = 100 };
                btn.Location = new Point(x, 0);
                btn.Click += (s, e) => SwitchSysTab(tags[idx]);
                pnlTabs.Controls.Add(btn);
                sysTabBtns[i] = btn;
                x += 105;
            }
            pg.Controls.Add(pnlTabs);

            var overview = new Card { Title = "Sistem Sağlık Özeti", Dock = DockStyle.Top, Height = 110, Padding = new Padding(16, 42, 16, 12) };
            lblScore = new Label { Font = UiKit.FontBig, ForeColor = UiKit.Ok, Text = "—", AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };
            lblScore.SetBounds(16, 40, 100, 60);
            overview.Controls.Add(lblScore);
            
            lblScoreDesc = new Label { Font = UiKit.FontBase, ForeColor = UiKit.Muted, AutoSize = false };
            lblScoreDesc.SetBounds(120, 44, 560, 60);
            overview.Controls.Add(lblScoreDesc);
            
            var bRef = new FlatButton { Text = "🔄 Yenile" };
            bRef.SetBounds(700, 40, 100, 32);
            bRef.Click += (s, e) => { RefreshSystemTabs(true); Log("Yenilendi."); };
            overview.Controls.Add(bRef);
            pg.Controls.Add(overview);

            var pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = UiKit.Surface, Padding = new Padding(20) };
            pnlSysContent = new Panel { Dock = DockStyle.Fill, BackColor = UiKit.Surface };
            pnlRight.Controls.Add(pnlSysContent);
            pg.Controls.Add(pnlRight);
            pnlRight.BringToFront();

            sysRefresh = new System.Windows.Forms.Timer { Interval = 30000 };
            sysRefresh.Tick += (s, e) => { if (curPage == 4 && Visible && (DateTime.Now - lastSysRefresh).TotalSeconds > 25) RefreshVisibleTab(false); };
            sysRefresh.Start();

            SwitchSysTab("rtbOverview");
            return pg;
        }

        void SwitchSysTab(string tag)
        {
            currentSysTab = tag;
            for (int i = 0; i < sysTabBtns.Length; i++)
            {
                sysTabBtns[i].Selected = (sysTabBtns[i].Tag.ToString() == tag);
                sysTabBtns[i].Invalidate();
            }
            RefreshVisibleTab(true);
        }

        internal void RefreshSystemTabs(bool force)
        {
            if (pnlSysContent == null) return;
            if (!force && (DateTime.Now - lastSysRefresh).TotalSeconds < 25) return;
            RefreshVisibleTab(force);
            HealthScore.Compute(this);
        }

        internal void RefreshVisibleTab(bool force)
        {
            if (pnlSysContent == null) return;
            pnlSysContent.SuspendLayout();
            pnlSysContent.Controls.Clear();

            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = UiKit.Surface, ForeColor = UiKit.Text, Font = UiKit.FontMono10, DetectUrls = false, WordWrap = false
            };
            pnlSysContent.Controls.Add(rtb);

            try
            {
                switch (currentSysTab)
                {
                    case "rtbOverview": SystemInfo.FillOverview(rtb); break;
                    case "rtbCPU": SystemInfo.FillCPU(rtb); break;
                    case "rtbRAM": SystemInfo.FillRAM(rtb); break;
                    case "rtbGPU": SystemInfo.FillGPU(rtb); break;
                    case "rtbDisk": SystemInfo.FillDisk(rtb); break;
                    case "rtbNet": SystemInfo.FillNet(rtb); break;
                    case "rtbPower": SystemInfo.FillPower(rtb); break;
                }
            }
            catch (Exception ex) { Log("[HATA] Sistem: " + ex.Message); }

            pnlSysContent.ResumeLayout();
            lastSysRefresh = DateTime.Now;
        }
    }
}