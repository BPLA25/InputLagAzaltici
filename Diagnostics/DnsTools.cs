using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using Microsoft.Win32;

namespace InputLagAzaltici
{
    static class DnsTools
    {
        public static void OnDnsTest(MainForm f)
        {
            if (f.lblDns != null) f.lblDns.Text = "Test ediliyor...";
            f.Log("DNS ping testi...");
            var t = new Thread(() =>
            {
                double bestAvg = double.MaxValue;
                int best = -1;
                for (int i = 0; i < f.dnsNames.Length; i++)
                {
                    long sum = 0;
                    int ok = 0;
                    using (var p = new Ping())
                    {
                        for (int k = 0; k < 5; k++)
                            try
                            {
                                var r = p.Send(f.dnsIps[i][0], 1000);
                                if (r.Status == IPStatus.Success) { ok++; sum += r.RoundtripTime; }
                            }
                            catch { }
                    }
                    if (ok == 0) { f.Log("[UYARI] " + f.dnsNames[i] + ": yanıt yok"); continue; }
                    double avg = (double)sum / ok;
                    f.Log(f.dnsNames[i] + ": ort " + avg.ToString("0.0") + " ms");
                    if (avg < bestAvg) { bestAvg = avg; best = i; }
                }
                f.bestDns = best;
                if (f.lblDns != null) f.BeginInvoke(new Action(() =>
                {
                    if (best >= 0) f.lblDns.Text = "En hızlı: " + f.dnsNames[best] + " (" + bestAvg.ToString("0.0") + " ms)";
                    else f.lblDns.Text = "Hiçbir DNS'ten yanıt alınamadı.";
                }));
            });
            t.IsBackground = true;
            t.Start();
        }

        public static List<NetworkInterface> ActiveAdapters()
        {
            var res = new List<NetworkInterface>();
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet && ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) continue;
                res.Add(ni);
            }
            return res;
        }

        public static void OnDnsApply(MainForm f)
        {
            if (f.bestDns < 0) { f.Log("Önce 'DNS ping testi'ni çalıştır."); return; }
            f.backupStore.Load();
            string o;
            int n = 0;
            foreach (var ni in ActiveAdapters())
            {
                try
                {
                    var ns = Registry.GetValue(Consts.HKLM + "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\" + ni.Id, "NameServer", null);
                    string cur = ns == null ? "" : ns.ToString().Trim();
                    f.backupStore.Save("dns|" + ni.Name, cur.Length == 0 ? "DHCP" : cur.Replace(' ', ','));
                    ProcessRunner.RunPublic("netsh", "interface ip set dns name=\"" + ni.Name + "\" static " + f.dnsIps[f.bestDns][0] + " primary", out o);
                    ProcessRunner.RunPublic("netsh", "interface ip add dns name=\"" + ni.Name + "\" " + f.dnsIps[f.bestDns][1] + " index=2", out o);
                    n++;
                }
                catch (Exception ex) { f.Log("[HATA] DNS: " + ex.Message); }
            }
            f.backupStore.SaveFile();
            f.Log("[OK] DNS " + f.dnsNames[f.bestDns] + " ayarlandı (" + n + " bağlantı).");
        }

        public static void OnDnsRestore(MainForm f)
        {
            f.backupStore.Load();
            int n = 0;
            foreach (var k in new List<string>(f.backupStore.Backup.Keys))
            {
                if (!k.StartsWith("dns|")) continue;
                string key = k;
                string v = f.backupStore.Backup[key];
                if (f.Do("DNS geri: " + key.Substring(4), () => RestoreDnsPublic(key.Substring(4), v))) { f.backupStore.Backup.Remove(k); n++; }
            }
            f.backupStore.SaveFile();
            if (n == 0) f.Log("Geri alınacak DNS yedeği yok.");
        }

        public static void RestoreDnsPublic(string name, string v)
        {
            string o;
            if (v == "DHCP") { ProcessRunner.RunPublic("netsh", "interface ip set dns name=\"" + name + "\" source=dhcp", out o); return; }
            var ips = v.Split(',');
            ProcessRunner.RunPublic("netsh", "interface ip set dns name=\"" + name + "\" static " + ips[0] + " primary", out o);
            for (int i = 1; i < ips.Length; i++)
                ProcessRunner.RunPublic("netsh", "interface ip add dns name=\"" + name + "\" " + ips[i] + " index=" + (i + 1), out o);
        }
    }
}