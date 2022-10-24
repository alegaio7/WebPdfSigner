using System.Collections.Generic;

namespace DesktopModule
{
    public class SignHashesWebRequest
    {
        public string CertificateThumbprint { get; set; }
        public List<SignHashFileInfo> FileHashesToSign { get; set; }
    }
}