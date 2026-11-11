using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace InputLagAzaltici
{
    static class DiskTweaks
    {
        public static string TrimState()
        {
            string o;
            ProcessRunner.RunCmd("fsutil", "behavior query DisableDeleteNotify", out o);
            var m = Regex.Match(o, "DisableDeleteNotify\\s*=\\s*(\\d)");
            return m.Success ? m.Groups[1].Value : "";
        }

        public static void ApplyTrim(BackupStore b)
        {
            string s = TrimState();
            if (s == "0") return;
            if (s == "") throw new Exception("TRIM durumu okunamadı.");
            b.Save("trim", "1");
            string o;
            ProcessRunner.RunCmd("fsutil", "behavior set DisableDeleteNotify 0", out o);
        }

        public static bool CheckTrim() { return TrimState() == "0"; }

        public static void ApplyPagefile(BackupStore b, int mb)
        {
            string o;
            ProcessRunner.RunPsPublic("(Get-CimInstance Win32_ComputerSystem).AutomaticManagedPagefile", out o);
            string st = o.Trim().Split('\n')[0].Trim();
            if (st == "True" || st == "False") b.Save("pagefile|auto", st);
            ProcessRunner.RunPsPublic("$cs = Get-CimInstance Win32_ComputerSystem; Set-CimInstance -InputObject $cs -Property @{AutomaticManagedPagefile=$false}", out o);
            string drive = Path.GetPathRoot(Environment.SystemDirectory);
            b.SetReg(Consts.HKLM + "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "PagingFiles",
                new[] { drive + "pagefile.sys " + mb + " " + mb }, RegistryValueKind.MultiString);
        }

        public static bool CheckPagefile(int mb)
        {
            var pf = Registry.GetValue(Consts.HKLM + "SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "PagingFiles", null) as string[];
            return pf != null && pf.Length > 0 && pf[0].ToLower().EndsWith(" " + mb + " " + mb);
        }
    }
}