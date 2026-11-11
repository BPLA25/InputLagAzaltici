using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace InputLagAzaltici
{
    class BackupStore
    {
        public Dictionary<string, string> Backup = new Dictionary<string, string>();
        readonly string backupFile;

        public BackupStore(string file) { backupFile = file; }

        public void Load()
        {
            try
            {
                Backup.Clear();
                if (!File.Exists(backupFile)) return;
                foreach (var line in File.ReadAllLines(backupFile))
                {
                    int i = line.IndexOf('=');
                    if (i > 0) Backup[line.Substring(0, i)] = line.Substring(i + 1);
                }
            }
            catch { }
        }

        public void SaveFile()
        {
            try
            {
                var lines = new List<string>();
                foreach (var kv in Backup) lines.Add(kv.Key + "=" + kv.Value);
                File.WriteAllLines(backupFile, lines.ToArray());
            }
            catch { }
        }

        public void Save(string key, string value)
        { if (!Backup.ContainsKey(key)) Backup[key] = value; }

        public void SetReg(string fullKey, string name, object value, RegistryValueKind kind)
        {
            object old = Registry.GetValue(fullKey, name, null);
            string ov;
            if (old == null) ov = "NULL";
            else if (old is string[]) ov = string.Join("~~", (string[])old);
            else ov = old.ToString();
            Save("reg|" + fullKey + "|" + name + "|" + kind, ov);
            Registry.SetValue(fullKey, name, value, kind);
        }

        public void Restore(string key, string v)
        {
            string o;
            if (key == "mouse") { var p = v.Split(','); Native.SystemParametersInfo(0x0004, 0, new[] { int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]) }, 3); }
            else if (key == "mousespeed") Native.SystemParametersInfo(0x0071, 0, new IntPtr(int.Parse(v)), 3);
            else if (key == "keydelay") Native.SystemParametersInfo(0x0017, (uint)int.Parse(v), IntPtr.Zero, 3);
            else if (key == "keyspeed") Native.SystemParametersInfo(0x000B, (uint)int.Parse(v), IntPtr.Zero, 3);
            else if (key == "power") ProcessRunner.RunPublic("powercfg", "/setactive " + v, out o);
            else if (key == "hpet") { if (v == "yes") ProcessRunner.RunPublic(ProcessRunner.BcdPath, "/set useplatformclock true", out o); else ProcessRunner.RunPublic(ProcessRunner.BcdPath, "/deletevalue useplatformclock", out o); }
            else if (key == "hibernate") { if (v == "1") ProcessRunner.RunPublic("powercfg", "/hibernate on", out o); }
            else if (key == "memcomp") { if (v == "True") ProcessRunner.RunPsPublic("Enable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue", out o); }
            else if (key == "hz") DisplayTweaks.SetRefreshRate(uint.Parse(v));
            else if (key == "nicpm") { if (v == "Enabled") ProcessRunner.RunPsPublic("Get-NetAdapter -Physical | Set-NetAdapterPowerManagement -AllowComputerToTurnOffDevice Enabled -ErrorAction SilentlyContinue", out o); }
            else if (key == "pagefile|auto") { if (v == "True") ProcessRunner.RunPsPublic("$cs = Get-CimInstance Win32_ComputerSystem; Set-CimInstance -InputObject $cs -Property @{AutomaticManagedPagefile=$true}", out o); }
            else if (key == "trim") { if (v == "1") ProcessRunner.RunPublic("fsutil", "behavior set DisableDeleteNotify 1", out o); }
            else if (key.StartsWith("svcstart|")) Registry.SetValue(Consts.HKLM + "SYSTEM\\CurrentControlSet\\Services\\" + key.Substring(9), "Start", int.Parse(v), RegistryValueKind.DWord);
            else if (key.StartsWith("svc|")) { if (v == "Running") ProcessRunner.StartServicePublic(key.Substring(4), 5); }
            else if (key.StartsWith("dns|")) DnsTools.RestoreDnsPublic(key.Substring(4), v);
            else if (key.StartsWith("reg|"))
            {
                var p = key.Split('|');
                var full = p[1];
                var name = p[2];
                var kind = (RegistryValueKind)Enum.Parse(typeof(RegistryValueKind), p[3]);
                if (v == "NULL") DeleteValue(full, name);
                else if (kind == RegistryValueKind.DWord) Registry.SetValue(full, name, int.Parse(v), kind);
                else if (kind == RegistryValueKind.QWord) Registry.SetValue(full, name, long.Parse(v), kind);
                else if (kind == RegistryValueKind.MultiString) Registry.SetValue(full, name, v.Split(new[] { "~~" }, StringSplitOptions.None), kind);
                else Registry.SetValue(full, name, v, kind);
            }
        }

        void DeleteValue(string fullKey, string name)
        {
            try { using (var rk = RegistryHelper.OpenFull(fullKey, true)) if (rk != null) rk.DeleteValue(name, false); } catch { }
        }
    }
}