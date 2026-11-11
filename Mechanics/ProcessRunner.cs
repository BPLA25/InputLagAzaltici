using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace InputLagAzaltici
{
    static class ProcessRunner
    {
        public static readonly string BcdPath = ResolveBcd();

        static string ResolveBcd()
        {
            var w = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var p = Path.Combine(w, "sysnative", "bcdedit.exe");
            return File.Exists(p) ? p : Path.Combine(w, "System32", "bcdedit.exe");
        }

        public static int RunPublic(string f, string a, out string o) { return RunCmd(f, a, out o); }

        public static int RunPublicPwr(string a, out string o) { return RunCmd("powercfg", a, out o); }

        public static int RunPsPublic(string s, out string o)
        { return RunCmd("powershell", "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"" + s + "\"", out o); }

        public static int RunCmd(string fileName, string args, out string output)
        {
            output = "";
            try
            {
                var psi = new ProcessStartInfo(fileName, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var p = Process.Start(psi))
                {
                    output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    return p.ExitCode;
                }
            }
            catch (Exception ex) { output = ex.Message; return -1; }
        }

        public static bool ServiceRunning(string name)
        {
            try { using (var sc = new ServiceController(name)) return sc.Status == ServiceControllerStatus.Running; }
            catch { return false; }
        }

        public static void StopService(string name, int waitSec)
        {
            try
            {
                using (var sc = new ServiceController(name))
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(waitSec));
                    }
            }
            catch { }
        }

        public static void StartServicePublic(string name, int waitSec)
        {
            try
            {
                using (var sc = new ServiceController(name))
                    if (sc.Status == ServiceControllerStatus.Stopped)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(waitSec));
                    }
            }
            catch { }
        }
    }
}