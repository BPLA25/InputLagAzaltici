using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace InputLagAzaltici
{
    static class SettingsStore
    {
        public static void Save(MainForm f)
        {
            try
            {
                var l = new List<string>();
                foreach (var o in f.opts) l.Add("opt." + o.Id + "=" + (o.Box.Checked ? "1" : "0"));
                foreach (var kv in f.misc) l.Add("misc." + kv.Key + "=" + (kv.Value.Checked ? "1" : "0"));
                l.Add("pagefile=" + f.nudPagefile.Value.ToString(CultureInfo.InvariantCulture));
                l.Add("standbymb=" + f.nudStandby.Value.ToString(CultureInfo.InvariantCulture));
                l.Add("pinghost=" + f.txtPingHost.Text);
                l.Add("schedtime=" + f.dtSched.Value.ToString("HH:mm", CultureInfo.InvariantCulture));
                l.Add("schedprofile=" + f.cmbSched.SelectedIndex);
                l.Add("profile=" + f.cmbProfile.SelectedIndex);
                l.Add("applied=" + string.Join(",", new List<string>(f.applied).ToArray()));
                
                l.Add("theme=" + f.currentTheme); 
                
                // UYARIYI GİDERMEK İÇİN YEREL DEĞİŞKEN KULLANILDI
                Color accColor = f.selectedAccentColor;
                int accArgb = accColor.ToArgb();
                l.Add("accentColor=" + accArgb); 
                
                Color bgColor = f.customBgColor;
                int bgArgb = bgColor.ToArgb();
                l.Add("customBgColor=" + bgArgb); 
                
                File.WriteAllLines(f.settingsFile, l.ToArray());
            }
            catch { }
        }

        public static void Load(MainForm f)
        {
            f.loading = true;
            try
            {
                if (!File.Exists(f.settingsFile)) return;
                var d = new Dictionary<string, string>();
                foreach (var line in File.ReadAllLines(f.settingsFile))
                {
                    int i = line.IndexOf('=');
                    if (i > 0) d[line.Substring(0, i)] = line.Substring(i + 1);
                }
                string v;
                foreach (var o in f.opts) { if (!o.Applicable) continue; if (d.TryGetValue("opt." + o.Id, out v)) o.Box.Checked = (v == "1"); }
                foreach (var kv in f.misc) if (d.TryGetValue("misc." + kv.Key, out v)) kv.Value.Checked = (v == "1");
                int n;
                if (d.TryGetValue("pagefile", out v) && int.TryParse(v, out n)) f.nudPagefile.Value = Math.Max(f.nudPagefile.Minimum, Math.Min(f.nudPagefile.Maximum, n));
                if (d.TryGetValue("standbymb", out v) && int.TryParse(v, out n)) f.nudStandby.Value = Math.Max(f.nudStandby.Minimum, Math.Min(f.nudStandby.Maximum, n));
                if (d.TryGetValue("pinghost", out v) && v.Length > 0) f.txtPingHost.Text = v;
                if (d.TryGetValue("schedtime", out v)) { DateTime t; if (DateTime.TryParseExact(v, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out t)) f.dtSched.Value = t; }
                if (d.TryGetValue("schedprofile", out v) && int.TryParse(v, out n) && n >= 0 && n < f.cmbSched.Items.Count) f.cmbSched.SelectedIndex = n;
                if (d.TryGetValue("profile", out v) && int.TryParse(v, out n) && n >= 0 && n < f.cmbProfile.Items.Count) f.cmbProfile.SelectedIndex = n;
                
                if (d.TryGetValue("theme", out v) && int.TryParse(v, out n)) f.currentTheme = n;
                if (d.TryGetValue("accentColor", out v) && int.TryParse(v, out n)) f.selectedAccentColor = Color.FromArgb(n);
                if (d.TryGetValue("customBgColor", out v) && int.TryParse(v, out n)) f.customBgColor = Color.FromArgb(n);
                
                f.ApplyTheme();

                if (d.TryGetValue("applied", out v))
                {
                    f.applied.Clear();
                    foreach (var id in v.Split(','))
                        if (id.Length > 0 && f.optMap.ContainsKey(id)) f.applied.Add(id);
                }
            }
            catch { }
            finally { f.loading = false; }
        }

        public static void OnExport(MainForm f)
        {
            using (var dlg = new SaveFileDialog { Filter = "Ayar dosyası (*.ini)|*.ini", FileName = "inputlag_ayarlar.ini" })
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    Save(f);
                    try { File.Copy(f.settingsFile, dlg.FileName, true); } catch { }
                    f.Log("Ayarlar dışa aktarıldı.");
                }
        }

        public static void OnImport(MainForm f)
        {
            using (var dlg = new OpenFileDialog { Filter = "Ayar dosyası (*.ini)|*.ini" })
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try { File.Copy(dlg.FileName, f.settingsFile, true); } catch { }
                    Load(f);
                    f.ApplyTheme();
                    f.Log("Ayarlar içe aktarıldı.");
                }
        }

        public static void OnSaveLog(MainForm f)
        {
            using (var dlg = new SaveFileDialog { Filter = "Metin dosyası (*.txt)|*.txt", FileName = "log.txt" })
                if (dlg.ShowDialog() == DialogResult.OK)
                    try { File.WriteAllText(dlg.FileName, f.log.Text); f.Log("Log kaydedildi."); }
                    catch (Exception ex) { f.Log("[HATA] " + ex.Message); }
        }
    }
}