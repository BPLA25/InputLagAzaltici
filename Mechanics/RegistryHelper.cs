using Microsoft.Win32;

namespace InputLagAzaltici
{
    static class RegistryHelper
    {
        public static RegistryKey OpenFull(string full, bool write)
        {
            if (full.StartsWith(Consts.HKLM)) return Registry.LocalMachine.OpenSubKey(full.Substring(Consts.HKLM.Length), write);
            if (full.StartsWith(Consts.HKCU)) return Registry.CurrentUser.OpenSubKey(full.Substring(Consts.HKCU.Length), write);
            return null;
        }

        public static string RegS(string key, string name)
        {
            try
            {
                var o = Registry.GetValue(key, name, null);
                return o == null ? "" : o.ToString().Trim();
            }
            catch { return ""; }
        }

        public static int GetBuild()
        {
            try
            {
                var b = Registry.GetValue(Consts.HKLM + "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "CurrentBuild", null);
                int n;
                if (b != null && int.TryParse(b.ToString(), out n)) return n;
            }
            catch { }
            return 0;
        }
    }
}