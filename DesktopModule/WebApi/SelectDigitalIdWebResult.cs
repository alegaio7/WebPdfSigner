using DesktopModule.Controllers;

namespace DesktopModule
{
    public class SelectDigitalIdWebResult : ControllerResultBase
    {
        public string CertificateEncodedBase64 { get; set; }
        public string CertificateFriendlyName { get; set; }
    }
}