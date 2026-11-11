using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace InputLagAzaltici
{
    static class SystemInfo
    {
        static readonly int MemSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.MS));

        static void W(RichTextBox r, string t, Color? c = null)
        { if (r == null) return; r.SelectionStart = r.TextLength; r.SelectionColor = c ?? UiKit.Text; r.AppendText(t + "\n"); }

        static void KV(RichTextBox r, string k, string v, Color? c = null)
        {
            if (r == null) return;
            r.SelectionStart = r.TextLength; r.SelectionColor = UiKit.Muted;
            r.AppendText("  " + k.PadRight(32) + ": ");
            r.SelectionColor = c ?? UiKit.Text; r.AppendText(v + "\n");
        }

        static void Bullet(RichTextBox r, string t, Color c)
        { if (r == null) return; r.SelectionStart = r.TextLength; r.SelectionColor = c; r.AppendText("    • "); r.SelectionColor = UiKit.Text; r.AppendText(t + "\n"); }

        static void Sec(RichTextBox r, string t)
        { W(r, ""); W(r, "  ▸ " + t.ToUpper(), UiKit.Accent); W(r, "  " + new string('─', 95), UiKit.Border); }

        public static void FillOverview(RichTextBox r)
        {
            if (r == null) return; r.SuspendLayout(); r.Clear();
            Sec(r, "Genel Bakış");
            string nt = Consts.HKLM + "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion";
            KV(r, "İşletim Sistemi", RegistryHelper.RegS(nt, "ProductName") + " " + RegistryHelper.RegS(nt, "DisplayVersion"));
            KV(r, "Derleme (Build)", RegistryHelper.RegS(nt, "CurrentBuild") + "." + RegistryHelper.RegS(nt, "UBR"));
            KV(r, "Mimari", Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");
            KV(r, "Bilgisayar Adı", Environment.MachineName);
            KV(r, "Kullanıcı Adı", Environment.UserName);
            ulong u = Native.GetTickCount64() / 1000UL;
            KV(r, "Çalışma Süresi", (u / 3600UL) + " saat " + ((u % 3600UL) / 60UL) + " dakika");

            var ms = new Native.MS { len = (uint)MemSize }; Native.GlobalMemoryStatusEx(ref ms);
            Sec(r, "Donanım");
            KV(r, "İşlemci (CPU)", RegistryHelper.RegS(Consts.HKLM + "HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0", "ProcessorNameString"));
            KV(r, "Çekirdek Sayısı", Environment.ProcessorCount + " çekirdek");
            KV(r, "Fiziksel Bellek (RAM)", (ms.tp / 1073741824.0).ToString("0.00") + " GB (Boş: " + (ms.ap / 1073741824.0).ToString("0.00") + " GB)");
            try { uint c, b; DisplayTweaks.GetHzCached(out c, out b); KV(r, "Monitör Yenileme Hızı", c + " Hz (Maks: " + b + " Hz)"); } catch { }
            
            Sec(r, "Sağlık Uyarıları");
            int issues = 0;
            if (ms.load > 85) { Bullet(r, "RAM kullanımı %" + ms.load + " — çok yüksek", UiKit.Err); issues++; }
            try { if (!Native.IsProcessorFeaturePresent(40)) { Bullet(r, "AVX2 komut seti yok — bazı yeni oyunlar çalışmayabilir", UiKit.Warn); issues++; } } catch { }
            try { if (!Native.IsProcessorFeaturePresent(39)) { Bullet(r, "AVX komut seti yok", UiKit.Warn); issues++; } } catch { }
            var fs = Registry.GetValue(Consts.HKLM + "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power", "HiberbootEnabled", null);
            if (fs != null && fs.ToString() == "1") { Bullet(r, "Hızlı Başlatma açık — kapatılması önerilir", UiKit.Warn); issues++; }
            if (issues == 0) Bullet(r, "Sisteminizde kritik bir sorun bulunmadı.", UiKit.Ok);
            r.ResumeLayout();
        }

        public static void FillCPU(RichTextBox r)
        {
            if (r == null) return; r.SuspendLayout(); r.Clear();
            string k = Consts.HKLM + "HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0";
            Sec(r, "İşlemci (CPU) Bilgisi");
            KV(r, "Model", RegistryHelper.RegS(k, "ProcessorNameString"));
            KV(r, "Üretici", RegistryHelper.RegS(k, "VendorIdentifier"));
            KV(r, "Tanımlayıcı", RegistryHelper.RegS(k, "Identifier"));
            KV(r, "Mevcut Frekans", RegistryHelper.RegS(k, "~MHz") + " MHz");
            KV(r, "Mantıksal Çekirdek", Environment.ProcessorCount.ToString());
            
            Sec(r, "Desteklenen Komut Setleri");
            uint[] codes = { 3, 6, 10, 13, 36, 37, 38, 39, 40, 12, 21, 22 };
            string[] names = { "MMX", "SSE", "SSE2", "SSE3", "SSSE3", "SSE4.1", "SSE4.2", "AVX", "AVX2", "DEP (NX)", "SSE4a", "AES-NI" };
            for (int i = 0; i < codes.Length; i++)
            {
                bool h = false; try { h = Native.IsProcessorFeaturePresent(codes[i]); } catch { }
                KV(r, names[i], h ? "Destekleniyor" : "Desteklenmiyor", h ? UiKit.Ok : UiKit.Err);
            }
            r.ResumeLayout();
        }

        public static void FillRAM(RichTextBox r)
        {
            if (r == null) return; r.SuspendLayout(); r.Clear();
            var ms = new Native.MS { len = (uint)MemSize }; Native.GlobalMemoryStatusEx(ref ms);
            Sec(r, "Bellek (RAM) Kullanımı");
            KV(r, "Toplam Fiziksel Bellek", (ms.tp / 1073741824.0).ToString("0.00") + " GB");
            KV(r, "Kullanılabilir Bellek", (ms.ap / 1073741824.0).ToString("0.00") + " GB");
            KV(r, "Kullanım Oranı", "%" + ms.load, ms.load > 85 ? UiKit.Err : (ms.load > 65 ? UiKit.Warn : UiKit.Ok));
            KV(r, "Toplam Sanal Bellek", (ms.tpf / 1073741824.0).ToString("0.00") + " GB");
            KV(r, "Kullanılabilir Sanal Bellek", (ms.apf / 1073741824.0).ToString("0.00") + " GB");
            long f, s; if (MemoryTweaks.ReadMemLists(out f, out s)) { Sec(r, "Önbellek ve Boş Bellek"); KV(r, "Boş RAM", f + " MB"); KV(r, "Standby List (Önbellek)", s + " MB"); }
            r.ResumeLayout();
        }

        public static void FillGPU(RichTextBox r)
        {
            if (r == null) return; r.SuspendLayout(); r.Clear();
            Sec(r, "Ekran Kartı Bilgileri");
            bool any = false;
            try
            {
                using (var rk = Registry.LocalMachine.OpenSubKey(Consts.GpuClassKey))
                {
                    if (rk != null) foreach (var sn in rk.GetSubKeyNames())
                    {
                        if (!Regex.IsMatch(sn, "^\\d{4}$")) continue;
                        using (var sk = rk.OpenSubKey(sn))
                        {
                            if (sk == null) continue; var desc = sk.GetValue("DriverDesc"); if (desc == null) continue;
                            any = true; W(r, ""); W(r, "  ── " + desc.ToString().ToUpper() + " ──", UiKit.Purple);
                            KV(r, "Sürücü Sürümü", (sk.GetValue("DriverVersion") ?? "").ToString());
                            KV(r, "Sürücü Tarihi", (sk.GetValue("DriverDate") ?? "").ToString());
                            var vr = sk.GetValue("HardwareInformation.qwMemorySize");
                            if (vr != null) try { KV(r, "Bellek (VRAM)", (Convert.ToUInt64(vr) / 1073741824.0).ToString("0.0") + " GB"); } catch { }
                            KV(r, "Sağlayıcı", (sk.GetValue("ProviderName") ?? "").ToString());
                        }
                    }
                }
            } catch { }
            if (!any) W(r, "  Ekran kartı bilgisi okunamadı.", UiKit.Warn);
            Sec(r, "Monitör");
            try { uint c, b; DisplayTweaks.GetHzCached(out c, out b); KV(r, "Mevcut Yenileme Hızı", c + " Hz"); KV(r, "Maksimum Yenileme Hızı", b + " Hz", c >= b ? UiKit.Ok : UiKit.Warn); } catch { }
            r.ResumeLayout();
        }

        // 🚀 DİSK DONMASINI ÇÖZEN ASENKRON METOT
        public static void FillDisk(RichTextBox r)
        {
            if (r == null) return;
            r.SuspendLayout();
            r.Clear();
            Sec(r, "Sabit Disk (Disk) Sürücüleri");
            
            Task.Run(() =>
            {
                string result = "";
                try
                {
                    var drives = DriveInfo.GetDrives();
                    foreach (var di in drives)
                    {
                        if (!di.IsReady || di.DriveType != DriveType.Fixed) continue;
                        long fg = di.AvailableFreeSpace / 1073741824;
                        long tg = di.TotalSize / 1073741824;
                        int pct = (int)((1 - (double)di.AvailableFreeSpace / di.TotalSize) * 100);
                        
                        result += "\n  ── " + di.Name + " ──\n";
                        result += "  " + "Dosya Sistemi".PadRight(32) + ": " + di.DriveFormat + "\n";
                        result += "  " + "Toplam Alan".PadRight(32) + ": " + tg + " GB\n";
                        result += "  " + "Boş Alan".PadRight(32) + ": " + fg + " GB\n";
                        result += "  " + "Doluluk Oranı".PadRight(32) + ": %" + pct + "\n";
                    }
                }
                catch (Exception ex) { result += "  Hata: " + ex.Message + "\n"; }

                string trimText = "Bilinmiyor";
                try
                {
                    string o; ProcessRunner.RunPublic("fsutil", "behavior query DisableDeleteNotify", out o);
                    var m = Regex.Match(o, "DisableDeleteNotify\\s*=\\s*(\\d)");
                    if (m.Success) trimText = m.Groups[1].Value == "0" ? "TRIM açık" : "TRIM KAPALI";
                }
                catch { }

                if (r != null && !r.IsDisposed)
                {
                    r.Invoke(new Action(() =>
                    {
                        r.SelectionStart = r.TextLength;
                        r.SelectionColor = UiKit.Text;
                        r.AppendText(result);
                        Sec(r, "TRIM Desteği (SSD)");
                        KV(r, "DisableDeleteNotify", trimText, trimText.Contains("açık") ? UiKit.Ok : UiKit.Warn);
                        r.ResumeLayout();
                    }));
                }
            });
        }

        public static void FillNet(RichTextBox r)
        {
            if (r == null) return; r.SuspendLayout(); r.Clear();
            Sec(r, "Ağ Adaptörleri");
            try
            {
                bool found = false;
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet && ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) continue;
                    string desc = ni.Description.ToLower();
                    if (desc.Contains("virtual") || desc.Contains("pseudo") || desc.Contains("wfp") || desc.Contains("qos") || desc.Contains("filter") || desc.Contains("miniport")) continue;
                    found = true;
                    W(r, ""); W(r, "  ── " + ni.Name + " ──", UiKit.Purple);
                    KV(r, "Bağlantı Tipi", ni.NetworkInterfaceType.ToString());
                    KV(r, "Bağlantı Hızı", (ni.Speed / 1000000) + " Mbps");
                    KV(r, "MAC Adresi", ni.GetPhysicalAddress().ToString());
                    KV(r, "Açıklama", ni.Description);
                    try { foreach (var d in ni.GetIPProperties().DnsAddresses) KV(r, "DNS Sunucusu", d.ToString()); } catch { }
                }
                if (!found) W(r, "  Aktif fiziksel ağ adaptörü bulunamadı.", UiKit.Warn);
            } catch (Exception ex) { W(r, "  Hata: " + ex.Message, UiKit.Err); }
            r.ResumeLayout();
        }

        public static void FillPower(RichTextBox r)
        {
            if (r == null) return; r.SuspendLayout(); r.Clear();
            Sec(r, "Güç Planı ve Ayarları");
            KV(r, "Aktif Plan", PowerTweaks.ActivePlan());
            var h = Registry.GetValue(Consts.HKLM + "SYSTEM\\CurrentControlSet\\Control\\Power", "HibernateEnabled", null);
            KV(r, "Hazırda Beklet (Hibernate)", h != null && h.ToString() == "1" ? "Açık" : "Kapalı");
            var fs = Registry.GetValue(Consts.HKLM + "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power", "HiberbootEnabled", null);
            KV(r, "Hızlı Başlatma (Fast Startup)", fs != null && fs.ToString() == "1" ? "Açık" : "Kapalı");
            Sec(r, "Timer Çözünürlüğü");
            uint mn, mx, cr; Native.NtQueryTimerResolution(out mn, out mx, out cr);
            KV(r, "Mevcut Çözünürlük", (cr / 10000.0).ToString("0.000") + " ms", cr <= 5000 ? UiKit.Ok : UiKit.Warn);
            KV(r, "Minimum Çözünürlük", (mn / 10000.0).ToString("0.000") + " ms");
            KV(r, "Maksimum Çözünürlük", (mx / 10000.0).ToString("0.000") + " ms");
            r.ResumeLayout();
        }
    }
}