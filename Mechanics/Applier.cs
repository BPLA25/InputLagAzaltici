using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace InputLagAzaltici
{
    static class Applier
    {
        public static void ApplySelected(MainForm f, bool interactive)
        {
            if (f.busy) return;
            f.busy = true;
            try
            {
                f.backupStore.Load();
                f.errCount = 0;
                if (interactive && f.chkRestorePoint.Checked) Cleanup.CreateRestorePoint(f);

                var sel = new List<Opt>();
                foreach (var id in Consts.ApplyOrder)
                {
                    Opt o;
                    if (f.optMap.TryGetValue(id, out o) && o.Box.Checked && o.Applicable) sel.Add(o);
                }

                if (f.headerProgress != null)
                {
                    f.headerProgress.Visible = true; 
                    f.headerProgress.Minimum = 0; 
                    f.headerProgress.Maximum = sel.Count > 0 ? sel.Count : 1; 
                    f.headerProgress.Value = 0;
                }
                f.SetBusy(true);

                foreach (var o in sel)
                {
                    f.SetStatus(o.Short + "...", UiKit.Accent);
                    Opt oo = o;
                    if (f.Do(oo.Short, oo.Apply)) 
                    {
                        f.applied.Add(oo.Id); 
                        f.failedOpts.Remove(oo.Id);
                    }
                    else
                    {
                        f.failedOpts.Add(oo.Id);
                    }
                    if (f.headerProgress != null && f.headerProgress.Value < f.headerProgress.Maximum) f.headerProgress.Value++;
                    Application.DoEvents();
                }

                f.backupStore.SaveFile();
                SettingsStore.Save(f);
                f.UpdateHeader();

                if (f.errCount == 0) 
                {
                    f.SetStatus("Tamamlandı", UiKit.Ok);
                }
                else 
                {
                    f.SetStatus(f.errCount + " hata ile bitti", UiKit.Err);
                    MessageBox.Show(f.errCount + " adet ayar uygulanamadı.\nHangi ayarların başarısız olduğunu görmek için yanındaki kırmızı noktalara bakabilirsiniz.\n\nDetaylı hata kaydı: hata.log", "Bazı Ayarlar Uygulanamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                f.Log("Bitti. Bazı ayarlar için yeniden başlatma gerekebilir.");
                f.RefreshStatuses();
                if (f.curPage == 4) f.RefreshSystemTabs(true);
            }
            finally
            {
                f.busy = false; f.SetBusy(false);
                if (f.headerProgress != null) f.headerProgress.Visible = false;
            }

            if (f.chkAutoRollback.Checked && f.errCount >= Consts.ErrorRollbackLimit)
            {
                f.Warn(f.errCount + " hata, otomatik geri alınıyor.");
                RevertAll(f, false);
            }
        }

        public static void RevertAll(MainForm f, bool interactive)
        {
            if (f.busy) return;
            f.busy = true;
            try
            {
                f.SetTimer(false);
                f.backupStore.Load();
                if (f.backupStore.Backup.Count == 0)
                {
                    f.Log("Yedek bulunamadı (zaten orijinal)."); 
                    f.SetStatus("Hazır", UiKit.Ok);
                    f.applied.Clear(); f.failedOpts.Clear(); SettingsStore.Save(f); f.UpdateHeader();
                    return;
                }
                var keys = new List<string>(f.backupStore.Backup.Keys);
                var ordered = new List<string>();
                foreach (var k in keys) if (k.StartsWith("svcstart|")) ordered.Add(k);
                foreach (var k in keys) if (!k.StartsWith("svcstart|") && !k.StartsWith("svc|")) ordered.Add(k);
                foreach (var k in keys) if (k.StartsWith("svc|")) ordered.Add(k);

                f.errCount = 0; 
                f.SetBusy(true); 
                f.SetStatus("Geri alınıyor...", UiKit.Accent);
                
                if (f.headerProgress != null)
                {
                    f.headerProgress.Visible = true; 
                    f.headerProgress.Minimum = 0; 
                    f.headerProgress.Maximum = ordered.Count > 0 ? ordered.Count : 1; 
                    f.headerProgress.Value = 0;
                }
                foreach (var key in ordered)
                {
                    string k = key; string v = f.backupStore.Backup[key];
                    f.Do("Geri al: " + ShortKey(k), () => f.backupStore.Restore(k, v));
                    if (f.headerProgress != null && f.headerProgress.Value < f.headerProgress.Maximum) f.headerProgress.Value++;
                    Application.DoEvents();
                }
                try { File.Delete(f.backupFile); } catch { }
                f.backupStore.Backup.Clear(); f.applied.Clear(); f.failedOpts.Clear();
                SettingsStore.Save(f); f.UpdateHeader();
                if (f.errCount == 0) f.SetStatus("Geri alındı", UiKit.Ok); else f.SetStatus(f.errCount + " hata ile bitti", UiKit.Err);
                f.Log("Orijinal ayarlar geri yüklendi."); f.RefreshStatuses();
            }
            finally
            {
                f.busy = false; f.SetBusy(false);
                if (f.headerProgress != null) f.headerProgress.Visible = false;
            }
        }

        static string ShortKey(string k) { if (k.StartsWith("reg|")) { var p = k.Split('|'); return p[2]; } return k; }

        public static void VerifyDrift(MainForm f, bool interactive)
        {
            if (f.busy) return;
            f.busy = true;
            int fixedCount = 0;
            try
            {
                f.backupStore.Load();
                foreach (var o in f.opts)
                {
                    if (!f.applied.Contains(o.Id) || o.Check == null) continue;
                    bool ok; try { ok = o.Check(); } catch { continue; }
                    if (!ok) 
                    { 
                        Opt oo = o; 
                        if (f.Do("Kayma düzeltiliyor: " + oo.Short, oo.Apply)) f.failedOpts.Remove(oo.Id); 
                        fixedCount++; 
                    }
                }
                if (fixedCount > 0) f.backupStore.SaveFile();
                if (interactive || fixedCount > 0) f.Log(fixedCount == 0 ? "Tüm ayarlar yerinde." : fixedCount + " ayar yeniden uygulandı.");
                f.RefreshStatuses();
            }
            finally { f.busy = false; }
        }
    }
}