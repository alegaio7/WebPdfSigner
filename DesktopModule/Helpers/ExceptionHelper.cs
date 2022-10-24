using System.Text;

namespace DesktopModule.Helpers
{
    public static class ExceptionHelper
    {
        public static string GetFullMessage(this Exception ex)
        {
            if (ex is null)
                return "No exception!";

            var sb = new StringBuilder();
            var e = ex;
            while (e != null)
            {
                if (sb.Length > 0)
                    sb.Append(Environment.NewLine);
                sb.Append(e.Message);
                e = e.InnerException;
            }
            return sb.ToString();
        }
    }
}