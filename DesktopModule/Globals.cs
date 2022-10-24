using DesktopModule.Helpers;
using System.Reflection;
using System.Resources;

namespace DesktopModule
{
    public static class Globals
    {
        public const string DEFAULT_LANGUAGE = "en";

        internal static string _currentLang = DEFAULT_LANGUAGE;

        private static ResourceManager _rm;

        public const int DEFAULT_WEBAPI_PORT = 20202;

        public enum EventType
        {
            Info,
            Warning,
            Error
        }

        public const string STRING_RESOURCES = "DesktopModule.Strings";
        public const string CONFIG_FILE = "DesktopModule.settings.txt";

        public static string M_ERROR;
        public static string M_INFO;

        static Globals() {
            CultureHelper.SetLanguage(Globals._currentLang);
            _rm = new ResourceManager(Globals.STRING_RESOURCES, Assembly.GetExecutingAssembly());
            M_ERROR = _rm.GetString("GLOBALS_M_ERROR");
            M_INFO = _rm.GetString("GLOBALS_M_INFORMATION");
        }
    }
}