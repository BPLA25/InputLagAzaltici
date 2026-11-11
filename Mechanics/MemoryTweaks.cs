using System;
using System.Runtime.InteropServices;

namespace InputLagAzaltici
{
    static class MemoryTweaks
    {
        static IntPtr memBuf = IntPtr.Zero;
        static long cachedFree, cachedStd;
        static DateTime cachedTime = DateTime.MinValue;

        public static void ApplyStandbyList()
        {
            EnablePrivilege("SeProfileSingleProcessPrivilege");
            int size = Marshal.SizeOf(typeof(int));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.WriteInt32(ptr, 4);
                int st = Native.NtSetSystemInformation(80, ptr, size);
                if (st != 0) throw new Exception("0x" + st.ToString("X8"));
            }
            finally { Marshal.FreeHGlobal(ptr); }
        }

        public static bool ReadMemLists(out long f, out long s)
        {
            f = 0; s = 0;
            try
            {
                const int count = 22;
                if (memBuf == IntPtr.Zero) memBuf = Marshal.AllocHGlobal(count * IntPtr.Size);
                int ret;
                int st = Native.NtQuerySystemInformation(80, memBuf, count * IntPtr.Size, out ret);
                if (st != 0) return false;
                long z = Marshal.ReadIntPtr(memBuf, 0).ToInt64();
                long fr = Marshal.ReadIntPtr(memBuf, IntPtr.Size).ToInt64();
                long stb = 0;
                for (int i = 5; i < 13; i++) stb += Marshal.ReadIntPtr(memBuf, i * IntPtr.Size).ToInt64();
                f = (z + fr) * 4096 / 1048576;
                s = stb * 4096 / 1048576;
                return true;
            }
            catch { return false; }
        }

        public static bool ReadMemListsCached(out long f, out long s)
        {
            if ((DateTime.Now - cachedTime).TotalMilliseconds > 2000 && ReadMemLists(out cachedFree, out cachedStd))
                cachedTime = DateTime.Now;
            f = cachedFree;
            s = cachedStd;
            return cachedTime != DateTime.MinValue;
        }

        public static void ApplyMemoryCompression(BackupStore b)
        {
            string o;
            ProcessRunner.RunPsPublic("(Get-MMAgent).MemoryCompression", out o);
            string st = o.Trim().Split('\n')[0].Trim();
            if (st == "True" || st == "False") b.Save("memcomp", st);
            ProcessRunner.RunPsPublic("Disable-MMAgent -MemoryCompression -ErrorAction SilentlyContinue", out o);
        }

        static void EnablePrivilege(string name)
        {
            IntPtr tok;
            if (!Native.OpenProcessToken(Native.GetCurrentProcess(), 0x28, out tok)) return;
            try
            {
                long luid;
                if (!Native.LookupPrivilegeValue(null, name, out luid)) return;
                var tp = new Native.TP { c = 1, l = luid, a = 2 };
                Native.AdjustTokenPrivileges(tok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
            }
            finally { Native.CloseHandle(tok); }
        }
    }
}