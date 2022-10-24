namespace DesktopModule
{
    public static class VersionInfo
    {
        public static int MajorVersion { get; set; } = 1;
        public static int MinorVersion { get; set; } = 0;
        public static int Revision { get; set; } = 3;

        public static string GetVersionString()
        {
            return $"{MajorVersion}.{MinorVersion}.{Revision}";
        }
    }
}

// Rev 3:       2022-10-06      ADD: Added English resources
// Rev 2:       2022-09-21      ADD: Closing warning