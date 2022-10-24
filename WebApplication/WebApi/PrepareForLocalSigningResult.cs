namespace WebTestApplication.WebApi
{
    public class PrepareForLocalSigningResult
    {
        public string CertificateThumbprint { get; set; }
        public List<FilePreparedForSigning> FileHashesToSign { get; set; }
    }
}
