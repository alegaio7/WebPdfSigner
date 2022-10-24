using DesktopModule.Helpers;

namespace DesktopModule
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && !string.IsNullOrEmpty(args[0])) {
                var lang = args[0].ToLower();
                if (lang == "/es")
                    Globals._currentLang = "es";
            }
            CultureHelper.SetLanguage(Globals._currentLang);
            ApplicationConfiguration.Initialize();
            Application.Run(new Main());
        }
    }
}