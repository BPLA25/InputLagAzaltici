using System;
using System.IO;
using System.Threading;

namespace InputLagAzaltici
{
    static class Cleanup
    {
        public static void OnCleanTemp(MainForm f)
        {
            f.Log("Geçici dosya temizliği...");
            var t = new Thread(() =>
            {
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string[] dirs = {
                    Path.GetTempPath(),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
                    Path.Combine(local, "NVIDIA\\DXCache"),
                    Path.Combine(local, "NVIDIA\\GLCache"),
                    Path.Combine(local, "D3DSCache"),
                    Path.Combine(local, "AMD\\DxCache"),
                    Path.Combine(local, "AMD\\GLCache")
                };
                long freed = 0;
                foreach (string d in dirs) try { if (Directory.Exists(d)) DeleteDirContents(d, ref freed); } catch { }
                f.Log("[OK] Temizlik: " + (freed / 1048576) + " MB boşaltıldı.");
            });
            t.IsBackground = true;
            t.Start();
        }

        static void DeleteDirContents(string dir, ref long freed)
        {
            foreach (string f in Directory.GetFiles(dir)) try { long len = new FileInfo(f).Length; File.Delete(f); freed += len; } catch { }
            foreach (string sub in Directory.GetDirectories(dir))
                try { DeleteDirContents(sub, ref freed); Directory.Delete(sub, false); } catch { }
        }

        public static void CreateRestorePoint(MainForm f)
        {
            var old = System.Windows.Forms.Cursor.Current;
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
            f.SetStatus("Geri yükleme noktası...", UiKit.Accent);
            System.Windows.Forms.Application.DoEvents();
            string o;
            int code = ProcessRunner.RunPsPublic("Checkpoint-Computer -Description 'InputLagAzaltici' -RestorePointType MODIFY_SETTINGS -ErrorAction Stop", out o);
            System.Windows.Forms.Cursor.Current = old;
            if (code == 0) f.Log("[OK] Geri yükleme noktası oluşturuldu.");
            else f.Warn("Geri yükleme noktası oluşturulamadı: " + o.Trim());
        }
    }
}