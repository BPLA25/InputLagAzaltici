using System;
using System.Runtime.InteropServices;

namespace InputLagAzaltici
{
    static class Native
    {
        [DllImport("ntdll.dll")] public static extern int NtSetTimerResolution(uint d, bool s, out uint c);
        [DllImport("ntdll.dll")] public static extern int NtQueryTimerResolution(out uint min, out uint max, out uint cur);
        [DllImport("ntdll.dll")] public static extern int NtSetSystemInformation(int cls, IntPtr info, int len);
        [DllImport("ntdll.dll")] public static extern int NtQuerySystemInformation(int cls, IntPtr info, int len, out int ret);
        [DllImport("user32.dll")] public static extern bool SystemParametersInfo(uint a, uint p, int[] v, uint w);
        [DllImport("user32.dll")] public static extern bool SystemParametersInfo(uint a, uint p, IntPtr v, uint w);

        [StructLayout(LayoutKind.Sequential)] public struct SK { public uint cbSize, dwFlags; }
        [StructLayout(LayoutKind.Sequential)] public struct FK { public uint cbSize, dwFlags, w, d, r, b; }
        [StructLayout(LayoutKind.Sequential)] public struct TK { public uint cbSize, dwFlags; }

        [DllImport("user32.dll")] public static extern bool SystemParametersInfo(uint a, uint p, ref SK v, uint w);
        [DllImport("user32.dll")] public static extern bool SystemParametersInfo(uint a, uint p, ref FK v, uint w);
        [DllImport("user32.dll")] public static extern bool SystemParametersInfo(uint a, uint p, ref TK v, uint w);

        [StructLayout(LayoutKind.Sequential)]
        public struct MS { public uint len, load; public ulong tp, ap, tpf, apf, tv, av, aev; }

        [DllImport("kernel32.dll")] public static extern bool GlobalMemoryStatusEx(ref MS m);
        [DllImport("kernel32.dll")] public static extern bool IsProcessorFeaturePresent(uint f);
        [DllImport("kernel32.dll")] public static extern uint SetThreadExecutionState(uint f);
        [DllImport("kernel32.dll")] public static extern bool GetSystemTimes(out long i, out long k, out long u);
        [DllImport("kernel32.dll")] public static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll")] public static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32.dll")] public static extern ulong GetTickCount64();

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct TP { public int c; public long l; public int a; }

        [DllImport("advapi32.dll")] public static extern bool OpenProcessToken(IntPtr p, uint a, out IntPtr t);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)] public static extern bool LookupPrivilegeValue(string s, string n, out long l);
        [DllImport("advapi32.dll")] public static extern bool AdjustTokenPrivileges(IntPtr t, bool d, ref TP n, int l, IntPtr p, IntPtr r);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DM
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string name;
            public ushort sv, dv, sz, dx;
            public uint f;
            public int px, py;
            public uint orient, fixedOut;
            public short color, dup, yr, tt, col;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string form;
            public ushort logpx;
            public uint bpp, w, h, flags, hz;
            public uint icm, intent, media, dither, r1, r2, pw, ph;
        }

        [DllImport("user32.dll", CharSet = CharSet.Ansi)] public static extern bool EnumDisplaySettings(string d, int n, ref DM m);
        [DllImport("user32.dll", CharSet = CharSet.Ansi)] public static extern int ChangeDisplaySettings(ref DM m, uint f);
    }
}