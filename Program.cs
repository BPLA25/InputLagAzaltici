using System;
using System.IO;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;

namespace InputLagAzaltici
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            bool admin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            if (!admin)
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo(Application.ExecutablePath)
                    {
                        Verb = "runas",
                        UseShellExecute = true,
                        Arguments = string.Join(" ", args)
                    };
                    System.Diagnostics.Process.Start(psi);
                }
                catch { MessageBox.Show("Yonetici izni gerekli."); }
                return;
            }

            bool created;
            var mtx = new Mutex(true, "InputLagAzaltici_SingleInstance", out created);
            if (!created) { MessageBox.Show("Program zaten calisiyor."); return; }

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, ex) =>
            {
                try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hata.log"), DateTime.Now + " " + ex.Exception + "\r\n"); } catch { }
                MessageBox.Show("Hata: " + ex.Exception.Message);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                try { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hata.log"), DateTime.Now + " " + ex.ExceptionObject + "\r\n"); } catch { }
            };

            bool startTray = false;
            foreach (string a in args) if (a.ToLower() == "/tray") startTray = true;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Uygulama ikonunu pencereye ve görev çubuğuna uygula
            var form = new MainForm(startTray);
            try 
            { 
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath)) form.Icon = new System.Drawing.Icon(iconPath); 
            } 
            catch { }
            
            Application.Run(form);
            GC.KeepAlive(mtx);
        }
    }
}