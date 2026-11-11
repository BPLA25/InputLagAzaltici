using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;

namespace InputLagAzaltici
{
    static class SpeedTest
    {
        public static void OnSpeedTest(MainForm f)
        {
            if (f.lblSpeed != null) f.lblSpeed.Text = "Test ediliyor...";
            f.Log("İnternet hız testi...");
            var t = new Thread(() =>
            {
                try
                {
                    long ping = -1;
                    using (var p = new Ping())
                    {
                        try
                        {
                            var r = p.Send("1.1.1.1", 1500);
                            if (r.Status == IPStatus.Success) ping = r.RoundtripTime;
                        }
                        catch { }
                    }
                    double downMbps = -1, upMbps = -1;
                    try
                    {
                        var url = "https://speed.cloudflare.com/__down?bytes=10000000";
                        var sw = Stopwatch.StartNew();
                        var req = (HttpWebRequest)WebRequest.Create(url);
                        req.Timeout = 15000;
                        req.ReadWriteTimeout = 15000;
                        long bytes = 0;
                        using (var resp = req.GetResponse())
                        using (var st = resp.GetResponseStream())
                        {
                            var buf = new byte[65536];
                            int n;
                            while ((n = st.Read(buf, 0, buf.Length)) > 0)
                            {
                                bytes += n;
                                if (sw.ElapsedMilliseconds > 15000) break;
                            }
                        }
                        sw.Stop();
                        if (bytes > 0 && sw.Elapsed.TotalSeconds > 0) downMbps = bytes * 8 / 1_000_000.0 / sw.Elapsed.TotalSeconds;
                    }
                    catch (Exception ex) { f.Log("[UYARI] İndirme: " + ex.Message); }
                    try
                    {
                        var url = "https://speed.cloudflare.com/__up";
                        var data = new byte[2_000_000];
                        new Random().NextBytes(data);
                        var sw = Stopwatch.StartNew();
                        var req = (HttpWebRequest)WebRequest.Create(url);
                        req.Method = "POST";
                        req.ContentType = "application/octet-stream";
                        req.ContentLength = data.Length;
                        req.Timeout = 15000;
                        req.ReadWriteTimeout = 15000;
                        using (var st = req.GetRequestStream()) st.Write(data, 0, data.Length);
                        sw.Stop();
                        if (sw.Elapsed.TotalSeconds > 0) upMbps = data.Length * 8 / 1_000_000.0 / sw.Elapsed.TotalSeconds;
                    }
                    catch (Exception ex) { f.Log("[UYARI] Yükleme: " + ex.Message); }

                    string txt = "İndirme: " + (downMbps >= 0 ? downMbps.ToString("0.00") + " Mbps" : "-") +
                                 "   Yükleme: " + (upMbps >= 0 ? upMbps.ToString("0.00") + " Mbps" : "-") +
                                 "   Ping: " + (ping >= 0 ? ping + " ms" : "-");
                    if (f.lblSpeed != null) f.BeginInvoke(new Action(() => f.lblSpeed.Text = txt));
                    f.Log("[OK] Hız testi: " + txt);
                }
                catch (Exception ex)
                {
                    f.Log("[HATA] " + ex.Message);
                    if (f.lblSpeed != null) f.BeginInvoke(new Action(() => f.lblSpeed.Text = "Hata: " + ex.Message));
                }
            });
            t.IsBackground = true;
            t.Start();
        }
    }
}