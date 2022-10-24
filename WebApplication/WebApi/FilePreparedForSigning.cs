namespace WebTestApplication.WebApi
{
    public class FilePreparedForSigning
    {
        public string OriginalFile { get; set; }
        public string PreparedFile { get; set; }
        public byte[] FileHash { get; set; }
        public byte[] SignedFileHash { get; set; }
    }
}
