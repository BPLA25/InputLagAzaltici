using System;
using System.Drawing;
using System.Windows.Forms;

namespace InputLagAzaltici
{
    public partial class MainForm
    {
        Panel BuildSettingsPage()
        {
            var pg = MkPage();
            pg.AutoScroll = true;
            int y = 12;

            MkLabel(pg, "Profil:", 16, y + 4, UiKit.Muted);
            cmbProfile = MkCombo(pg, 66, y, 120, Consts.ProfileNames, 3);
            cmbProfile.SelectionChangeCommitted += (s, e) => SetProfile(cmbProfile.SelectedIndex);

            var bAll = new FlatButton { Text = "Tümünü seç" };
            bAll.SetBounds(196, y - 1, 100, 26);
            bAll.Click += (s, e) => { SetProfile(3); cmbProfile.SelectedIndex = 3; };
            pg.Controls.Add(bAll);

            var bNone = new FlatButton { Text = "Hiçbirini seçme" };
            bNone.SetBounds(302, y - 1, 120, 26);
            bNone.Click += (s, e) => { SetProfile(4); cmbProfile.SelectedIndex = 4; };
            pg.Controls.Add(bNone);

            MkLabel(pg, "☑ seçili = uygulanacak  •  ● = şu an aktif", 440, y + 4, UiKit.Muted);
            y += 40;

            Section(pg, "GİRİŞ AYGITLARI", ref y);
            AddOpt(pg, "mouse", "Fare ivmesi", "Fare ivmesini kapat (hız 6/11)", "Pointer precision kapatılır.", 0,
                () => InputTweaks.ApplyMouse(backupStore), InputTweaks.CheckMouse, ref y);
            AddOpt(pg, "key", "Klavye", "Klavye tekrar gecikmesi minimum, hız maksimum", "Tuşa basılı tutunca tekrar hızlı başlar.", 0,
                () => InputTweaks.ApplyKeyboard(backupStore), InputTweaks.CheckKeyboard, ref y);
            AddOpt(pg, "sticky", "Yapışkan Tuşlar", "Yapışkan / Filtre / Geçiş tuşlarını kapat", "5x Shift gibi kısayolları engeller.", 0,
                InputTweaks.ApplyStickyKeys, InputTweaks.CheckSticky, ref y);
            AddOpt(pg, "usb", "USB Selective Suspend", "USB Seçmeli Askıya Alma'yı kapat", "Fare/klavye USB uykuya geçmesin.", 1,
                PowerTweaks.ApplyUsbSuspend, null, ref y);

            Section(pg, "GÜÇ, İŞLEMCİ VE EKRAN", ref y);
            AddOpt(pg, "power", "Güç planı", "Yüksek Performans planı + CPU %100", "Güç planı Yüksek Performans olur.", 0,
                () => PowerTweaks.ApplyPower(backupStore), PowerTweaks.CheckPower, ref y);
            AddOpt(pg, "parking", "Core Parking", "CPU Core Parking'i kapat", "Çekirdeklerin uyutulmasını engeller.", 1,
                PowerTweaks.ApplyCoreParking, null, ref y);
            AddOpt(pg, "timer", "Timer çözünürlüğü", "Timer çözünürlüğü 0.5 ms (program açıkken)", "Program kapanınca eski haline döner.", 0,
                () => SetTimer(true), () => timerOn, ref y);
            AddOpt(pg, "hpet", "HPET Ayarı", "HPET sistem saatini optimize et (bcdedit)", "Yeniden başlatma gerekir.", 2,
                () => SystemTweaks.ApplyHpet(backupStore), null, ref y);
            AddOpt(pg, "hz", "Ekran yenileme hızı", "Ekran yenileme hızını maksimuma çıkar", "En yüksek Hz seçilir.", 1,
                () => DisplayTweaks.ApplyHz(backupStore), DisplayTweaks.CheckHz, ref y);

            Section(pg, "OYUN ÖNCELİĞİ", ref y);
            AddOpt(pg, "dvr", "Game DVR", "Game DVR / arka plan oyun kaydını kapat", "Xbox Game Bar kaydı kapanır.", 0,
                () => SystemTweaks.ApplyReg("dvr", tweaks, backupStore), () => SystemTweaks.GroupApplied("dvr", tweaks), ref y);
            AddOpt(pg, "mm", "MMCSS", "Oyun için MMCSS önceliği", "SystemResponsiveness=0.", 0,
                () => SystemTweaks.ApplyReg("mm", tweaks, backupStore), () => SystemTweaks.GroupApplied("mm", tweaks), ref y);
            AddOpt(pg, "prio", "CPU önceliği", "Ön plan uygulamaya CPU önceliği", "Win32PrioritySeparation=38.", 1,
                () => SystemTweaks.ApplyReg("prio", tweaks, backupStore), () => SystemTweaks.GroupApplied("prio", tweaks), ref y);

            Section(pg, "BELLEK VE AĞ", ref y);
            AddOpt(pg, "standby", "Standby List", "Standby List (RAM önbellek) bir kez temizle", "Boşta bekleyen önbelleği temizler.", 1,
                MemoryTweaks.ApplyStandbyList, null, ref y);
            AddOpt(pg, "memcomp", "Bellek Sıkıştırma", "Bellek Sıkıştırmayı kapat", "Az RAM'li sistemde ters etki yapabilir.", 2,
                () => MemoryTweaks.ApplyMemoryCompression(backupStore), null, ref y);
            AddOpt(pg, "network", "TCPNoDelay", "Ağ kartı TCPNoDelay / Nagle'ı kapat", "Oyun paket gecikmesini azaltır.", 1,
                () => NetworkTweaks.ApplyNetworkOptimization(backupStore), NetworkTweaks.CheckNetwork, ref y);
            AddOpt(pg, "nicpm", "Ağ kartı güç", "Ağ kartı güç tasarrufunu kapat", "Adaptörün uykuya geçmesini engeller.", 1,
                () => NetworkTweaks.ApplyNicPm(backupStore), null, ref y);

            Section(pg, "DİSK VE SİSTEM", ref y);
            AddOpt(pg, "visuals", "Görsel Efektler", "Windows görsel efektlerini kapat", "Animasyon/şeffaflık azalır.", 1,
                () => SystemTweaks.ApplyReg("visuals", tweaks, backupStore), () => SystemTweaks.GroupApplied("visuals", tweaks), ref y);
            AddOpt(pg, "services", "Arka Plan Servisleri", "Gereksiz servisleri durdur (SysMain, DiagTrack, DPS)", "Geri Al servisleri yeniden başlatır.", 2,
                () => ServiceTweaks.ApplyServices(backupStore), ServiceTweaks.CheckServices, ref y);
            AddOpt(pg, "hibernation", "Hazırda Beklet", "Hazırda Beklet'i tamamen kapat", "Hiberfil.sys silinir.", 2,
                () => PowerTweaks.ApplyHibernation(backupStore), PowerTweaks.CheckHibernation, ref y);
            AddOpt(pg, "faststart", "Hızlı Başlatma", "Hızlı Başlatma'yı (Fast Startup) kapat", "Gerçek tam kapanış.", 1,
                () => SystemTweaks.ApplyReg("faststart", tweaks, backupStore), () => SystemTweaks.GroupApplied("faststart", tweaks), ref y);
            AddOpt(pg, "bgapps", "Arka plan uygulamaları", "Windows arka plan uygulamalarını kapat", "Mağaza arka planını engeller.", 1,
                () => SystemTweaks.ApplyReg("bgapps", tweaks, backupStore), () => SystemTweaks.GroupApplied("bgapps", tweaks), ref y);
            AddOpt(pg, "search", "Arama indeksleme", "Windows Search indekslemeyi kapat", "Arka plan disk kullanımı azalır.", 2,
                () => ServiceTweaks.ApplySearch(backupStore), ServiceTweaks.CheckSearch, ref y);
            AddOpt(pg, "trim", "SSD TRIM", "SSD TRIM'i kontrol et ve kapalıysa aç", "Zaten açıksa hiçbir şey yapmaz.", 0,
                () => DiskTweaks.ApplyTrim(backupStore), DiskTweaks.CheckTrim, ref y);
            AddOpt(pg, "pagefile", "Sayfa dosyası", "Sayfa dosyasını sabit boyuta çek (MB):", "Yeniden başlatma gerekir.", 2,
                () => DiskTweaks.ApplyPagefile(backupStore, (int)nudPagefile.Value), () => DiskTweaks.CheckPagefile((int)nudPagefile.Value), ref y);
            nudPagefile = MkNum(pg, 326, y - 27, 80, 1024, 32768, 4096);

            Section(pg, "SES VE EKRAN KARTI (deneysel)", ref y);
            AddOpt(pg, "audiofx", "Ses iyileştirmeleri", "Ses iyileştirmelerini kapat", "Bazı cihazlarda uygulanamayabilir.", 2,
                () => GpuTweaks.ApplyAudioFx(backupStore), null, ref y);
            AddOpt(pg, "gpu", "NVIDIA güç modu", "NVIDIA: maksimum performans modu", "PowerMizer ayarı.", 2,
                () => GpuTweaks.ApplyGpu(backupStore), GpuTweaks.CheckGpu, ref y);

            MkLabel(pg, " ", 16, y + 6);
            return pg;
        }

        internal void SetProfile(int idx)
        {
            foreach (var o in opts)
            {
                if (!o.Applicable) continue;
                if (idx == 3) o.Box.Checked = true;
                else if (idx == 4) o.Box.Checked = false;
                else o.Box.Checked = (o.Level <= idx);
            }
            UpdateHeader();
        }
    }
}