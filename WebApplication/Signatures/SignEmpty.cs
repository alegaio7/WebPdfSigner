using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Syncfusion.Pdf.Security;

namespace WebTestApplication.Signatures
{
    internal class SignEmpty : IPdfExternalSigner
    {
        private string _hashAlgorithm;
        private byte[] _message;
        private X509Certificate2 _cert;

        public SignEmpty(string hashAlgorithm, X509Certificate2 cert = null)
        {
            _hashAlgorithm = hashAlgorithm;
            _cert = cert;
        }

        public string HashAlgorithm
        {
            get { return _hashAlgorithm; }
        }

        public byte[] Message { get { return _message; } }

        public byte[] Sign(byte[] message, out byte[] timeStampResponse)
        {
            _message = message;
            timeStampResponse = null;
            return null;
        }
    }
}
