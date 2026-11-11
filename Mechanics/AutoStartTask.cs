using System.Windows.Forms;

namespace InputLagAzaltici
{
    static class AutoStartTask
    {
        public static void SetAutoStart(MainForm f)
        {
            string o;
            if (f.chkAutoStart.Checked)
            {
                var exe = Application.ExecutablePath;
                var tr = "\\\"" + exe + "\\\" /tray";
                if (ProcessRunner.RunPublic("schtasks", "/create /tn " + Consts.TaskName + " /tr \"" + tr + "\" /sc onlogon /rl highest /f", out o) == 0)
                    f.Log("[OK] Otomatik başlatma eklendi.");
                else { f.Log("[HATA] " + o.Trim()); f.chkAutoStart.Checked = false; }
            }
            else
            {
                ProcessRunner.RunPublic("schtasks", "/delete /tn " + Consts.TaskName + " /f", out o);
                f.Log("Otomatik başlatma kaldırıldı.");
            }
        }

        public static void Check(MainForm f)
        {
            string o;
            int code = ProcessRunner.RunPublic("schtasks", "/query /tn " + Consts.TaskName, out o);
            bool old = f.loading;
            f.loading = true;
            f.chkAutoStart.Checked = (code == 0);
            f.loading = old;
        }
    }
}