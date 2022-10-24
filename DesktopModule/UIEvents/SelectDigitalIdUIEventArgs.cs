namespace DesktopModule
{
    public class SelectDigitalIdUIEventArgs : WebRequestCoordinatorBaseEventArgs
    {
        public string CertificateEncodedBase64 { get; set; }
        public string CertificateFriendlyName { get; set; }
    }
}