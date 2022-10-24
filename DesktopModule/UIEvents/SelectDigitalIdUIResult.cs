namespace DesktopModule
{
    public class SelectDigitalIdUIResult : WebRequestCoordinatorBaseResult
    {
        public string CertificateEncodedBase64 { get; set; }
        public string CertificateFriendlyName { get; set; }
    }
}