namespace WebTestApplication.WebApi
{
    public class PrepareForSigningRequest
    {
        public string File { get; set; }
        public string OriginalFile { get; set; }
        public string CertificateEncodedBase64 { get; set; }
        public string NameInSignature { get; set; }
        public byte[] SignatureImage { get; set; }
    }
}
