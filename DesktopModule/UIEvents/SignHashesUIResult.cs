using System.Collections.Generic;

namespace DesktopModule
{
    public class SignHashesUIResult : WebRequestCoordinatorBaseResult
    {
        public List<SignHashFileInfo> SignedHashes { get; set; }
    }
}