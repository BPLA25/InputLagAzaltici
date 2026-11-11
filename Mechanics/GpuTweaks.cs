using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace InputLagAzaltici
{
    static class GpuTweaks
    {
        public static void ApplyAudioFx(BackupStore b)
        {
            string bp = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\MMDevices\\Audio\\Render";
            int ok = 0, fail = 0;
            using (var rk = Registry.LocalMachine.OpenSubKey(bp))
            {
                if (rk == null) throw new Exception("Ses cihazi yok.");
                foreach (var id in rk.GetSubKeyNames())
                {
                    try
                    {
                        using (var fx = Registry.LocalMachine.OpenSubKey(bp + "\\" + id + "\\FxProperties"))
                            if (fx == null) continue;
                        b.SetReg(Consts.HKLM + bp + "\\" + id + "\\FxProperties", "{1da5d803-d492-4edd-8c23-e0c0ffee7f0e},5", 1, RegistryValueKind.DWord);
                        ok++;
                    }
                    catch { fail++; }
                }
            }
            if (ok == 0 && fail > 0) throw new Exception("Hicbir cihaza yazilamadi.");
        }

        public static List<string> NvidiaKeys()
        {
            var res = new List<string>();
            try
            {
                using (var rk = Registry.LocalMachine.OpenSubKey(Consts.GpuClassKey))
                {
                    if (rk == null) return res;
                    foreach (var sn in rk.GetSubKeyNames())
                    {
                        if (!Regex.IsMatch(sn, "^\\d{4}$")) continue;
                        using (var sk = rk.OpenSubKey(sn))
                        {
                            if (sk == null) continue;
                            var d = sk.GetValue("DriverDesc");
                            if (d != null && d.ToString().ToUpper().Contains("NVIDIA")) res.Add(Consts.HKLM + Consts.GpuClassKey + "\\" + sn);
                        }
                    }
                }
            }
            catch { }
            return res;
        }

        public static void ApplyGpu(BackupStore b)
        {
            var keys = NvidiaKeys();
            if (keys.Count == 0) { return; }
            foreach (var k in keys)
            {
                b.SetReg(k, "PerfLevelSrc", 0x2222, RegistryValueKind.DWord);
                b.SetReg(k, "PowerMizerEnable", 1, RegistryValueKind.DWord);
                b.SetReg(k, "PowerMizerLevel", 1, RegistryValueKind.DWord);
                b.SetReg(k, "PowerMizerLevelAC", 1, RegistryValueKind.DWord);
            }
        }

        public static bool CheckGpu()
        {
            var keys = NvidiaKeys();
            if (keys.Count == 0) return false;
            foreach (var k in keys)
            {
                var v = Registry.GetValue(k, "PerfLevelSrc", null);
                if (v == null || v.ToString() != "8738") return false;
            }
            return true;
        }
    }
}