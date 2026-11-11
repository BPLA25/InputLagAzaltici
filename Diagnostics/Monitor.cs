using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace InputLagAzaltici
{
    static class MonitorLogic
    {
        static readonly int MemSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.MS));
        static Dictionary<int, TimeSpan> cpuPrev = new Dictionary<int, TimeSpan>();
        static DateTime cpuPrevTime = DateTime.Now;

        public static void Tick(MainForm f)
        {
            f.monitorTicks++;
            bool needLive = (f.curPage == 2 && f.Visible); 
            if (!needLive && f.monitorTicks % 10 != 0) return;

            double cpu = CpuUsage(f);
            var ms = new Native.MS { len = (uint)MemSize };
            Native.GlobalMemoryStatusEx(ref ms);
            double totalGb = ms.tp / 1073741824.0;
            double availGb = ms.ap / 1073741824.0;
            double usedGb = totalGb - availGb;

            if (needLive && f.monitorTicks % 5 == 0 && !f.pingBusy)
            {
                f.pingBusy = true;
                try { f.pinger.SendAsync(f.txtPingHost.Text.Trim(), 1000, null); }
                catch { f.pingBusy = false; f.lastPing = -1; }
            }
            string pingText = f.lastPing >= 0 ? f.lastPing + " ms" : "-";

            if (f.chkStandbyAuto != null && f.chkStandbyAuto.Checked && f.monitorTicks % 10 == 0 && !f.busy)
            {
                long freeMb, stbMb;
                if (MemoryTweaks.ReadMemLists(out freeMb, out stbMb))
                {
                    if (freeMb < (long)f.nudStandby.Value && stbMb > 512 && (DateTime.Now - f.lastPurge).TotalSeconds > 30)
                    {
                        try { MemoryTweaks.ApplyStandbyList(); f.lastPurge = DateTime.Now; f.Log("Standby temizlendi (" + stbMb + " MB)."); }
                        catch (Exception ex) { f.lastPurge = DateTime.Now; f.Log("[HATA] Standby: " + ex.Message); }
                    }
                }
            }

            if (f.lblDashCpu != null)
            {
                if (f.lblDashCpu.InvokeRequired) f.lblDashCpu.BeginInvoke(new Action(() => UpdateDash(f, cpu, usedGb, pingText)));
                else UpdateDash(f, cpu, usedGb, pingText);
            }

            if (needLive && f.lblCpu != null)
            {
                uint mn, mx, cr; Native.NtQueryTimerResolution(out mn, out mx, out cr);
                f.lblCpu.Text = "CPU        : %" + cpu.ToString("0") + "  (" + Environment.ProcessorCount + " çekirdek)";
                f.lblRam.Text = "RAM        : " + usedGb.ToString("0.0") + " / " + totalGb.ToString("0.0") + " GB  (%" + ms.load + ")";
                long f2, s2;
                if (MemoryTweaks.ReadMemListsCached(out f2, out s2)) f.lblMem.Text = "Boş/Standby: " + f2 + " MB / " + s2 + " MB";
                else f.lblMem.Text = "Boş/Standby: okunamadı";
                f.lblTimerRes.Text = "Timer      : " + (cr / 10000.0).ToString("0.000") + " ms" + (f.timerOn ? "  (bu program ayarladı)" : "");
                f.lblPing.Text = "Ping       : " + pingText;
            }

            if (f.chkSched != null && f.chkSched.Checked && !f.busy && f.monitorTicks % 30 == 0)
            {
                string nowS = DateTime.Now.ToString("HH:mm");
                if (nowS == f.dtSched.Value.ToString("HH:mm") && f.lastSchedDay != DateTime.Today)
                {
                    f.lastSchedDay = DateTime.Today;
                    f.Log("Zamanlanmış profil: " + Consts.ProfileNames[f.cmbSched.SelectedIndex]);
                    f.SetProfile(f.cmbSched.SelectedIndex);
                    Applier.ApplySelected(f, false);
                }
            }

            if (f.chkVerify != null && f.chkVerify.Checked && f.monitorTicks % 1800 == 0 && f.applied.Count > 0)
                Applier.VerifyDrift(f, false);

            // 🚀 V4 YENİ: Arka plan uygulamalarını otomatik düşür (Her 10 saniyede bir)
            if (f.chkGmLow != null && f.chkGmLow.Checked && f.monitorTicks % 10 == 0)
            {
                string[] bgApps = { "chrome", "msedge", "firefox", "opera", "discord", "spotify", "steam" };
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (Array.IndexOf(bgApps, p.ProcessName.ToLower()) >= 0)
                        {
                            if (p.PriorityClass != ProcessPriorityClass.BelowNormal && p.PriorityClass != ProcessPriorityClass.Idle)
                            {
                                p.PriorityClass = ProcessPriorityClass.BelowNormal;
                            }
                        }
                    }
                    catch { }
                }
            }

            if (f.monitorTicks % 3 == 0) f.UpdateHeader();
        }

        private static void UpdateDash(MainForm f, double cpu, double usedGb, string pingText)
        {
            f.lblDashCpu.Text = "CPU: %" + cpu.ToString("0");
            f.lblDashRam.Text = "RAM: " + usedGb.ToString("0.0") + " GB";
            f.lblDashPing.Text = "Ping: " + pingText;
        }

        public static double CpuUsage(MainForm f)
        {
            if ((DateTime.Now - f.cachedCpuTime).TotalMilliseconds < 2000) return f.cachedCpu;
            long idle, kernel, user;
            if (!Native.GetSystemTimes(out idle, out kernel, out user)) return f.cachedCpu;
            long dIdle = idle - f.prevIdle, dKernel = kernel - f.prevKernel, dUser = user - f.prevUser;
            f.prevIdle = idle; f.prevKernel = kernel; f.prevUser = user;
            long total = dKernel + dUser;
            if (total <= 0) return f.cachedCpu;
            double v = (total - dIdle) * 100.0 / total;
            if (v < 0) v = 0;
            if (v > 100) v = 100;
            f.cachedCpu = v;
            f.cachedCpuTime = DateTime.Now;
            return v;
        }

        public static void OnPingTest(MainForm f)
        {
            string host = f.txtPingHost.Text.Trim();
            f.Log("Ping testi: " + host);
            var t = new System.Threading.Thread(() =>
            {
                try
                {
                    int ok = 0;
                    long min = long.MaxValue, max = 0, sum = 0;
                    using (var p = new System.Net.NetworkInformation.Ping())
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            try
                            {
                                var r = p.Send(host, 1000);
                                if (r.Status == System.Net.NetworkInformation.IPStatus.Success)
                                {
                                    ok++; sum += r.RoundtripTime;
                                    if (r.RoundtripTime < min) min = r.RoundtripTime;
                                    if (r.RoundtripTime > max) max = r.RoundtripTime;
                                }
                            }
                            catch { }
                            System.Threading.Thread.Sleep(150);
                        }
                    }
                    if (ok == 0) f.Log("[HATA] Ping yanıt yok.");
                    else f.Log("[OK] Ping " + host + ": min " + min + " / ort " + (sum / ok) + " / max " + max + " ms, kayıp %" + ((10 - ok) * 10));
                }
                catch (Exception ex) { f.Log("[HATA] Ping: " + ex.Message); }
            });
            t.IsBackground = true;
            t.Start();
        }

        public static void RefreshProcs(MainForm f)
        {
            if (f.lvProc == null) return;
            DateTime now = DateTime.Now;
            double elapsed = (now - cpuPrevTime).TotalSeconds;
            if (elapsed < 0.5) elapsed = 1;
            var newPrev = new Dictionary<int, TimeSpan>(64);
            var rows = new List<KeyValuePair<long, ListViewItem>>(128);
            
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    long ws = p.WorkingSet64;
                    TimeSpan t;
                    try { t = p.TotalProcessorTime; } catch { continue; }
                    double cpu = 0;
                    TimeSpan prev;
                    if (cpuPrev.TryGetValue(p.Id, out prev)) cpu = (t - prev).TotalSeconds / elapsed / Environment.ProcessorCount * 100.0;
                    newPrev[p.Id] = t;
                    
                    string prio;
                    try { prio = p.PriorityClass.ToString(); } catch { prio = "-"; }
                    
                    var it = new ListViewItem(p.ProcessName);
                    it.SubItems.Add(p.Id.ToString());
                    it.SubItems.Add((ws / 1048576).ToString());
                    it.SubItems.Add(cpu.ToString("0.0"));
                    it.SubItems.Add(prio);
                    it.Tag = p.Id;
                    rows.Add(new KeyValuePair<long, ListViewItem>(ws, it));
                }
                catch { }
            }
            
            cpuPrev = newPrev;
            cpuPrevTime = now;
            rows.Sort((a, b) => b.Key.CompareTo(a.Key));

            f.lvProc.BeginUpdate();
            f.lvProc.Items.Clear();
            int count = 0;
            foreach (var kv in rows)
            {
                if (count++ >= 60) break;
                f.lvProc.Items.Add(kv.Value);
            }
            f.lvProc.EndUpdate();
        }

        public static void SetSelectedProcPriority(MainForm f, ProcessPriorityClass pc)
        {
            if (f.lvProc.SelectedItems.Count == 0) { f.Log("Önce listeden bir işlem seç."); return; }
            int pid = (int)f.lvProc.SelectedItems[0].Tag;
            try { var p = Process.GetProcessById(pid); p.PriorityClass = pc; f.Log("[OK] " + p.ProcessName + " önceliği: " + pc); RefreshProcs(f); }
            catch (Exception ex) { f.Log("[HATA] " + ex.Message); }
        }

        public static void OnKillProc(MainForm f)
        {
            if (f.lvProc.SelectedItems.Count == 0) { f.Log("Önce listeden bir işlem seç."); return; }
            int pid = (int)f.lvProc.SelectedItems[0].Tag;
            string name = f.lvProc.SelectedItems[0].Text;
            if (MessageBox.Show("'" + name + "' kapatılsın mı?\r\nKaydedilmemiş veri kaybolabilir.", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            try { Process.GetProcessById(pid).Kill(); f.Log("[OK] Kapatıldı: " + name); RefreshProcs(f); }
            catch (Exception ex) { f.Log("[HATA] " + ex.Message); }
        }

        public static void RefreshStartup(MainForm f)
        {
            if (f.lvStartup == null) return;
            f.lvStartup.Items.Clear();
            string[][] src = {
                new[] { Consts.HKCU + "Software\\Microsoft\\Windows\\CurrentVersion\\Run", Consts.HKCU + "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run", "Kullanıcı" },
                new[] { Consts.HKLM + "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", Consts.HKLM + "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run", "Sistem" },
                new[] { Consts.HKLM + "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Run", Consts.HKLM + "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run32", "Sistem (32-bit)" }
            };
            foreach (var s in src)
            {
                try
                {
                    using (var rk = RegistryHelper.OpenFull(s[0], false))
                    {
                        if (rk == null) continue;
                        foreach (var name in rk.GetValueNames())
                        {
                            if (name.Length == 0) continue;
                            var cmd = rk.GetValue(name);
                            var it = new ListViewItem(name);
                            it.SubItems.Add(cmd == null ? "" : cmd.ToString());
                            it.SubItems.Add(s[2]);
                            it.SubItems.Add(IsStartupDisabled(s[1], name) ? "Kapalı" : "Açık");
                            it.Tag = new string[] { s[1], name };
                            f.lvStartup.Items.Add(it);
                        }
                    }
                }
                catch { }
            }
        }

        static bool IsStartupDisabled(string approvedKey, string name)
        {
            try { var b = Registry.GetValue(approvedKey, name, null) as byte[]; return b != null && b.Length > 0 && (b[0] & 1) == 1; }
            catch { return false; }
        }

        public static void OnToggleStartup(MainForm f)
        {
            if (f.lvStartup.SelectedItems.Count == 0) { f.Log("Önce listeden bir program seç."); return; }
            var tag = (string[])f.lvStartup.SelectedItems[0].Tag;
            try
            {
                bool dis = IsStartupDisabled(tag[0], tag[1]);
                var nb = new byte[12];
                nb[0] = (byte)(dis ? 2 : 3);
                Registry.SetValue(tag[0], tag[1], nb, RegistryValueKind.Binary);
                f.Log("[OK] Başlangıç: " + tag[1] + (dis ? " açıldı" : " kapatıldı"));
                RefreshStartup(f);
            }
            catch (Exception ex) { f.Log("[HATA] " + ex.Message); }
        }
    }
}