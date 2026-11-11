using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace InputLagAzaltici
{
    static class HealthScore
    {
        static readonly int MemSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.MS));

        public static void Compute(MainForm f)
        {
            if (f.lblScore == null) return;
            int score = 100;
            var list = new List<string>();
            try
            {
                int build = RegistryHelper.GetBuild();
                if (build > 0 && build < 19041) { score -= 15; list.Add("Windows eski sürüm"); }
                var ms = new Native.MS { len = (uint)MemSize };
                Native.GlobalMemoryStatusEx(ref ms);
                if (ms.load > 90) { score -= 20; list.Add("RAM > %90"); }
                else if (ms.load > 75) { score -= 8; list.Add("RAM yüksek"); }
                if (ms.tp < 4L * 1073741824) { score -= 10; list.Add("RAM < 4 GB"); }
                try { if (!Native.IsProcessorFeaturePresent(40)) { score -= 10; list.Add("AVX2 yok"); } } catch { }
                try { if (!Native.IsProcessorFeaturePresent(39)) { score -= 15; list.Add("AVX yok"); } } catch { }
                var fs = Registry.GetValue(Consts.HKLM + "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power", "HiberbootEnabled", null);
                if (fs != null && fs.ToString() == "1") { score -= 5; list.Add("Hızlı Başlatma açık"); }
            }
            catch { }
            if (score < 0) score = 0;
            f.lblScore.Text = score.ToString();
            f.lblScore.ForeColor = score >= 85 ? UiKit.Ok : score >= 65 ? UiKit.Warn : UiKit.Err;
            if (f.lblScoreDesc != null)
                f.lblScoreDesc.Text = score >= 85
                    ? "Sisteminiz oyun için iyi durumda."
                    : (list.Count > 0 ? "İyileştirilebilecek: " + string.Join(", ", list.ToArray()) : "Sistem iyi.");

            // Dashboard Skoru Güncellemesi (YENİ)
            if (f.lblDashScore != null)
            {
                f.lblDashScore.Text = score.ToString();
                f.lblDashScore.ForeColor = score >= 85 ? UiKit.Ok : score >= 65 ? UiKit.Warn : UiKit.Err;
                f.lblDashScoreDesc.Text = score >= 85 ? "Mükemmel" : score >= 65 ? "İyi" : "Geliştirilmeli";
            }
        }
    }
}