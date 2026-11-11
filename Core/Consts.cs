namespace InputLagAzaltici
{
    static class Consts
    {
        public const string HKLM = "HKEY_LOCAL_MACHINE\\";
        public const string HKCU = "HKEY_CURRENT_USER\\";
        public const string HighPerf = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
        public const string GpuClassKey = "SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e968-e325-11cd-bfc1-08002be10318}";
        public const string TaskName = "InputLagAzaltici";
        public const int ErrorRollbackLimit = 5;
        public const string GuidPattern = "[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}";

        public static readonly string[] ApplyOrder = {
            "power","parking","usb","timer","mouse","key","sticky","dvr","mm","prio",
            "standby","network","nicpm","visuals","services","hpet","hz","hibernation","faststart","bgapps",
            "search","trim","pagefile","audiofx","gpu","memcomp"
        };

        public static readonly string[] ProfileNames = { "Güvenli", "Dengeli", "Agresif", "Tümü", "Hiçbiri" };
    }
}