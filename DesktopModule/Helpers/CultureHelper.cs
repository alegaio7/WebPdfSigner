using System.Globalization;

namespace DesktopModule.Helpers
{
    public static class CultureHelper
    {
        public static void SetLanguage(string lang)
        {
            Thread.CurrentThread.CurrentUICulture = GetCultureInfoForLanguage(lang);
            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture;
        }

        public static CultureInfo GetCultureInfoForLanguage(string lang)
        {
            if (lang == "es")
                return new CultureInfo("es-AR"); // spanish-argentina
            else
                return new CultureInfo("en-US");
        }
    }
}