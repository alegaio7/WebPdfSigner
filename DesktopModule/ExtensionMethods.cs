using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopModule
{
    internal static class ExtensionMethods
    {
        public static bool IsNullOrEmptyString(this string value)
        {
            return string.IsNullOrEmpty(value);
        }
    }
}
