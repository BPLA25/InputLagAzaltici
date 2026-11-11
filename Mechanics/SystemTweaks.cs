using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace InputLagAzaltici
{
    static class SystemTweaks
    {
        public static List<RegTweak> InitTweaks()
        {
            string mm = Consts.HKLM + "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile";
            return new List<RegTweak>
            {
                new RegTweak("dvr", Consts.HKCU + "System\\GameConfigStore", "GameDVR_Enabled", 0, RegistryValueKind.DWord),
                new RegTweak("dvr", Consts.HKCU + "Software\\Microsoft\\Windows\\CurrentVersion\\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord),
                new RegTweak("mm", mm, "SystemResponsiveness", 0, RegistryValueKind.DWord),
                new RegTweak("mm", mm + "\\Tasks\\Games", "GPU Priority", 8, RegistryValueKind.DWord),
                new RegTweak("mm", mm + "\\Tasks\\Games", "Priority", 6, RegistryValueKind.DWord),
                new RegTweak("mm", mm + "\\Tasks\\Games", "Scheduling Category", "High", RegistryValueKind.String),
                new RegTweak("mm", mm + "\\Tasks\\Games", "SFIO Priority", "High", RegistryValueKind.String),
                new RegTweak("prio", Consts.HKLM + "SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "Win32PrioritySeparation", 38, RegistryValueKind.DWord),
                new RegTweak("visuals", Consts.HKCU + "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects", "VisualFXSetting", 2, RegistryValueKind.DWord),
                new RegTweak("faststart", Consts.HKLM + "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power", "HiberbootEnabled", 0, RegistryValueKind.DWord),
                new RegTweak("bgapps", Consts.HKCU + "Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled", 1, RegistryValueKind.DWord),
                new RegTweak("bgapps", Consts.HKCU + "Software\\Microsoft\\Windows\\CurrentVersion\\Search", "BackgroundAppGlobalToggle", 0, RegistryValueKind.DWord)
            };
        }

        public static void ApplyReg(string group, List<RegTweak> tweaks, BackupStore b)
        {
            foreach (var t in tweaks)
                if (t.Group == group) b.SetReg(t.Key, t.Name, t.Value, t.Kind);
        }

        public static bool GroupApplied(string group, List<RegTweak> tweaks)
        {
            foreach (var t in tweaks)
            {
                if (t.Group != group) continue;
                var c = Registry.GetValue(t.Key, t.Name, null);
                if (c == null || c.ToString() != t.Value.ToString()) return false;
            }
            return true;
        }

        public static void ApplyHpet(BackupStore b)
        {
            string o;
            ProcessRunner.RunCmd(ProcessRunner.BcdPath, "/enum {current}", out o);
            var m = Regex.Match(o, "^\\s*useplatformclock\\s+(\\S+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            string st = "default";
            if (m.Success && (m.Groups[1].Value.ToLower() == "yes" || m.Groups[1].Value.ToLower() == "true")) st = "yes";
            b.Save("hpet", st);
            if (ProcessRunner.RunCmd(ProcessRunner.BcdPath, "/set useplatformclock false", out o) != 0)
                throw new Exception("HPET kapatılamadı: " + o);
        }
    }
}