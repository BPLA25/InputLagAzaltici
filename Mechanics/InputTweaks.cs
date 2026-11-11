namespace InputLagAzaltici
{
    static class InputTweaks
    {
        static readonly int StickySize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.SK));
        static readonly int FilterSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.FK));
        static readonly int ToggleSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.TK));

        public static void ApplyMouse(BackupStore b)
        {
            var m = new int[3];
            Native.SystemParametersInfo(0x0003, 0, m, 0);
            b.Save("mouse", m[0] + "," + m[1] + "," + m[2]);
            var sp = new int[1];
            Native.SystemParametersInfo(0x0070, 0, sp, 0);
            b.Save("mousespeed", sp[0].ToString());
            Native.SystemParametersInfo(0x0004, 0, new[] { 0, 0, 0 }, 3);
            Native.SystemParametersInfo(0x0071, 0, new System.IntPtr(10), 3);
        }

        public static bool CheckMouse()
        {
            var m = new int[3];
            Native.SystemParametersInfo(0x0003, 0, m, 0);
            return m[2] == 0;
        }

        public static void ApplyKeyboard(BackupStore b)
        {
            var d = new int[1];
            var s = new int[1];
            Native.SystemParametersInfo(0x0016, 0, d, 0);
            Native.SystemParametersInfo(0x000A, 0, s, 0);
            b.Save("keydelay", d[0].ToString());
            b.Save("keyspeed", s[0].ToString());
            Native.SystemParametersInfo(0x0017, 0, System.IntPtr.Zero, 3);
            Native.SystemParametersInfo(0x000B, 31, System.IntPtr.Zero, 3);
        }

        public static bool CheckKeyboard()
        {
            var d = new int[1];
            var s = new int[1];
            Native.SystemParametersInfo(0x0016, 0, d, 0);
            Native.SystemParametersInfo(0x000A, 0, s, 0);
            return d[0] == 0 && s[0] == 31;
        }

        public static void ApplyStickyKeys()
        {
            var sk = new Native.SK { cbSize = (uint)StickySize, dwFlags = 506 };
            Native.SystemParametersInfo(0x003B, sk.cbSize, ref sk, 3);
            var fk = new Native.FK { cbSize = (uint)FilterSize, dwFlags = 122 };
            Native.SystemParametersInfo(0x0033, fk.cbSize, ref fk, 3);
            var tk = new Native.TK { cbSize = (uint)ToggleSize, dwFlags = 58 };
            Native.SystemParametersInfo(0x0035, tk.cbSize, ref tk, 3);
        }

        public static bool CheckSticky()
        {
            var sk = new Native.SK { cbSize = (uint)StickySize };
            Native.SystemParametersInfo(0x003A, sk.cbSize, ref sk, 0);
            return (sk.dwFlags & 5) == 0;
        }
    }
}