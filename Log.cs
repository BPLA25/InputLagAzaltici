using System;
using System.IO;

namespace InputLagAzaltici
{
    static class AppLogger
    {
        static readonly object _lock = new object();
        public static string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hata.log");

        public static void Info(string message) { Write("INFO", message, null); }
        public static void Warn(string message) { Write("UYARI", message, null); }
        public static void Error(string message, Exception ex = null) { Write("HATA", message, ex); }

        static void Write(string level, string message, Exception ex)
        {
            try
            {
                lock (_lock)
                {
                    string line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [" + level + "] " + message;
                    if (ex != null)
                    {
                        line += Environment.NewLine + "  İstisna: " + ex.GetType().FullName;
                        line += Environment.NewLine + "  Mesaj: " + ex.Message;
                        if (!string.IsNullOrEmpty(ex.StackTrace))
                            line += Environment.NewLine + "  Stack Trace:" + Environment.NewLine + ex.StackTrace;
                    }
                    line += Environment.NewLine;
                    File.AppendAllText(LogPath, line);
                }
            }
            catch { }
        }
    }
}