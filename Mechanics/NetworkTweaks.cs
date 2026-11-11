using Microsoft.Win32;

namespace InputLagAzaltici
{
    static class NetworkTweaks
    {
        public static void ApplyNetworkOptimization(BackupStore b)
        {
            string path = "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces";
            using (var rk = Registry.LocalMachine.OpenSubKey(path, false))
            {
                if (rk == null) return;
                foreach (var sub in rk.GetSubKeyNames())
                {
                    try
                    {
                        var full = Consts.HKLM + path + "\\" + sub;
                        b.SetReg(full, "TcpAckFrequency", 1, RegistryValueKind.DWord);
                        b.SetReg(full, "TCPNoDelay", 1, RegistryValueKind.DWord);
                    }
                    catch { }
                }
            }
        }

        public static bool CheckNetwork()
        {
            string path = "SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces";
            using (var rk = Registry.LocalMachine.OpenSubKey(path, false))
            {
                if (rk == null) return false;
                foreach (var sub in rk.GetSubKeyNames())
                {
                    var v = Registry.GetValue(Consts.HKLM + path + "\\" + sub, "TCPNoDelay", null);
                    if (v != null && v.ToString() == "1") return true;
                }
            }
            return false;
        }

        public static void ApplyNicPm(BackupStore b)
        {
            string o;
            ProcessRunner.RunPsPublic("(Get-NetAdapterPowerManagement | Select-Object -First 1).AllowComputerToTurnOffDevice", out o);
            string st = o.Trim().Split('\n')[0].Trim();
            if (st == "Enabled" || st == "Disabled" || st == "Unsupported") b.Save("nicpm", st);
            ProcessRunner.RunPsPublic("Get-NetAdapter -Physical | Set-NetAdapterPowerManagement -AllowComputerToTurnOffDevice Disabled -ErrorAction SilentlyContinue", out o);
        }
    }
}