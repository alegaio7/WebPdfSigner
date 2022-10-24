using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DesktopModule.Helpers
{
    public class SignHelper
    {
        public byte[] SignData(byte[] hash, X509Certificate2 cert)
        {
            var ka = cert.GetKeyAlgorithm();
            if (ka == "RSA" || ka == "1.2.840.113549.1.1.1")
            {
                using (var rsa = cert.GetRSAPrivateKey())
                {
                    return rsa.SignData(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }
            else if (ka == "1.2.840.10045.2.1")
            {
                using (var ec = cert.GetECDsaPrivateKey())
                {
                    return ec.SignData(hash, HashAlgorithmName.SHA256, System.Security.Cryptography.DSASignatureFormat.Rfc3279DerSequence);
                }
            }
            else
                throw new Exception("Unsupported key algorithm.");
        }
    }
}