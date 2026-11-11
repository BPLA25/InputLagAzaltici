using Microsoft.Win32;

namespace InputLagAzaltici
{
    static class ServiceTweaks
    {
        public static void ApplyServices(BackupStore b)
        {
            string[] list = { "SysMain", "DiagTrack", "DPS" };
            foreach (var n in list)
                if (ProcessRunner.ServiceRunning(n)) { b.Save("svc|" + n, "Running"); ProcessRunner.StopService(n, 2); }
        }

        public static bool CheckServices() { return !ProcessRunner.ServiceRunning("SysMain"); }

        public static void ApplySearch(BackupStore b)
        {
            string k = Consts.HKLM + "SYSTEM\\CurrentControlSet\\Services\\WSearch";
            var st = Registry.GetValue(k, "Start", null);
            if (st != null) b.Save("svcstart|WSearch", st.ToString());
            b.Save("svc|WSearch", ProcessRunner.ServiceRunning("WSearch") ? "Running" : "Stopped");
            Registry.SetValue(k, "Start", 4, RegistryValueKind.DWord);
            ProcessRunner.StopService("WSearch", 4);
        }

        public static bool CheckSearch() { return !ProcessRunner.ServiceRunning("WSearch"); }
    }
}
