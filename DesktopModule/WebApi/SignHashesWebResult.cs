using System.Collections.Generic;
using DesktopModule.Controllers;

namespace DesktopModule
{
    public class SignHashesWebResult : ControllerResultBase
    {
        public List<SignHashFileInfo> SignedHashes { get; set; }
    }
}