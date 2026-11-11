using System;

namespace InputLagAzaltici
{
    static class DisplayTweaks
    {
        static readonly int DmSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.DM));
        static uint cachedCur, cachedBest;
        static DateTime cachedHzTime = DateTime.MinValue;

        static Native.DM NewDm() { return new Native.DM { sz = (ushort)DmSize }; }

        public static void GetHz(out uint cur, out uint best)
        {
            var c = NewDm();
            if (!Native.EnumDisplaySettings(null, -1, ref c)) throw new Exception("Ekran okunamadı.");
            cur = c.hz;
            best = cur;
            for (int i = 0; i < 2000; i++)
            {
                var m = NewDm();
                if (!Native.EnumDisplaySettings(null, i, ref m)) break;
                if (m.w == c.w && m.h == c.h && m.bpp == c.bpp && m.hz > best) best = m.hz;
            }
        }

        public static void GetHzCached(out uint cur, out uint best)
        {
            if ((DateTime.Now - cachedHzTime).TotalMilliseconds > 500)
            {
                GetHz(out cachedCur, out cachedBest);
                cachedHzTime = DateTime.Now;
            }
            cur = cachedCur;
            best = cachedBest;
        }

        public static void SetRefreshRate(uint hz)
        {
            var c = NewDm();
            if (!Native.EnumDisplaySettings(null, -1, ref c)) throw new Exception("Ekran okunamadı.");
            c.hz = hz;
            c.f = 0x00400000;
            int t = Native.ChangeDisplaySettings(ref c, 2);
            if (t != 0) throw new Exception(hz + " Hz kabul edilmedi (" + t + ").");
            if (Native.ChangeDisplaySettings(ref c, 1) != 0) throw new Exception("Hz değiştirilemedi.");
            cachedHzTime = DateTime.MinValue;
        }

        public static void ApplyHz(BackupStore b)
        {
            uint c, bst;
            GetHz(out c, out bst);
            if (bst <= c) return;
            b.Save("hz", c.ToString());
            SetRefreshRate(bst);
        }

        public static bool CheckHz()
        {
            uint c, bst;
            GetHzCached(out c, out bst);
            return c >= bst;
        }
    }
}