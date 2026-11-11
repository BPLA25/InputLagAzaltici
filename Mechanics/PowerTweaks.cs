using System;
using System.Text.RegularExpressions;

namespace InputLagAzaltici
{
    static class PowerTweaks
    {
        static string cachedActivePlan = "";
        static DateTime cachedPlanTime = DateTime.MinValue;

        public static void ApplyPower(BackupStore b)
        {
            string o;
            ProcessRunner.RunPublicPwr("/getactivescheme", out o);
            var m = Regex.Match(o, Consts.GuidPattern);
            if (m.Success) b.Save("power", m.Value);
            string scheme = Consts.HighPerf;
            if (ProcessRunner.RunPublicPwr("/setactive " + scheme, out o) != 0)
            {
                ProcessRunner.RunPublicPwr("-duplicatescheme " + Consts.HighPerf, out o);
                var d = Regex.Match(o, Consts.GuidPattern);
                if (!d.Success) throw new Exception("Yüksek performans planı oluşturulamadı.");
                scheme = d.Value;
                ProcessRunner.RunPublicPwr("/setactive " + scheme, out o);
            }
            ProcessRunner.RunPublicPwr("/setacvalueindex " + scheme + " SUB_PROCESSOR PROCTHROTTLEMIN 100", out o);
            ProcessRunner.RunPublicPwr("/setacvalueindex " + scheme + " 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4738-b2a0-5bf4a0d4d9d8 0", out o);
            ProcessRunner.RunPublicPwr("/setactive " + scheme, out o);
            cachedPlanTime = DateTime.MinValue;
        }

        public static bool CheckPower()
        {
            var p = ActivePlan().ToLower();
            return p.Contains("high perf") || p.Contains(Consts.HighPerf);
        }

        public static string ActivePlan()
        {
            if ((DateTime.Now - cachedPlanTime).TotalSeconds > 5)
            {
                string o;
                ProcessRunner.RunPublicPwr("/getactivescheme", out o);
                cachedActivePlan = o.Trim();
                cachedPlanTime = DateTime.Now;
            }
            return cachedActivePlan;
        }

        public static void ApplyCoreParking()
        {
            string o;
            ProcessRunner.RunPublicPwr("/setacvalueindex SCHEME_CURRENT SUB_PROCESSOR 0cc5b647-c1df-4637-891a-dec35c318583 100", out o);
            ProcessRunner.RunPublicPwr("/setactive SCHEME_CURRENT", out o);
            cachedPlanTime = DateTime.MinValue;
        }

        public static void ApplyUsbSuspend()
        {
            string o;
            ProcessRunner.RunPublicPwr("/setacvalueindex SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4738-b2a0-5bf4a0d4d9d8 0", out o);
            ProcessRunner.RunPublicPwr("/setactive SCHEME_CURRENT", out o);
            cachedPlanTime = DateTime.MinValue;
        }

        public static void ApplyHibernation(BackupStore b)
        {
            string o;
            object cur = Microsoft.Win32.Registry.GetValue(Consts.HKLM + "SYSTEM\\CurrentControlSet\\Control\\Power", "HibernateEnabled", null);
            if (cur != null) b.Save("hibernate", cur.ToString());
            ProcessRunner.RunPublic("powercfg", "/hibernate off", out o);
        }

        public static bool CheckHibernation()
        {
            object cur = Microsoft.Win32.Registry.GetValue(Consts.HKLM + "SYSTEM\\CurrentControlSet\\Control\\Power", "HibernateEnabled", null);
            return cur != null && cur.ToString() == "0";
        }
    }
}