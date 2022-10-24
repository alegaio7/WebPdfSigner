using Syncfusion.Pdf.Security;

namespace WebTestApplication.Signatures
{
    internal class ExternalSigner : IPdfExternalSigner
    {
        private string _hashAlgorithm;
        private byte[] _signedHash;

        public ExternalSigner(string hashAlgorithm, byte[] signedHash)
        {
            _hashAlgorithm = hashAlgorithm;
            _signedHash = signedHash;
        }

        public string HashAlgorithm
        {
            get { return _hashAlgorithm; }
        }

        public byte[] Sign(byte[] message, out byte[] timeStampResponse)
        {
            timeStampResponse = null;
            return _signedHash;
        }
    }
}
