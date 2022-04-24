using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Common.Cipher
{
    /// <summary>
    /// ECC 521bit X.509
    /// </summary>
    public class X509
    {
        static string DEFAULT_PFX_FILENAME = "private.pfx";
        static string DEFAULT_REQ = "CN=SimpleSecureChatServer";

        private static string? GetCurrentDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static X509Certificate2 LoadDefault()
        {
            string? currentPath = GetCurrentDirectory();
            if (currentPath == null)
            {
                throw new Exception("Could not get current path");
            }
            return Load(Path.Combine(currentPath, DEFAULT_PFX_FILENAME), exportable: true);
        }

        public static X509Certificate2 Load(string filename, bool exportable = false)
        {
            if (exportable)
            {
                return new X509Certificate2(filename, (string?)null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
            }
            return new X509Certificate2(filename);
        }

        public static X509Certificate2 Load(byte[] data, bool exportable = false)
        {
            if (exportable)
            {
                return new X509Certificate2(data, (string?)null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
            }
            return new X509Certificate2(data);
        }

        public static void GenerateDefault()
        {
            string? currentPath = GetCurrentDirectory();
            if (currentPath == null)
            {
                throw new Exception("Could not get current path");
            }

            Generate(Path.Combine(currentPath, DEFAULT_PFX_FILENAME), DEFAULT_REQ);
        }

        public static void Generate(string pfxPath, string subjectName)
        {
            var ecdsa = ECDsa.Create();
            var req = new CertificateRequest(subjectName, ecdsa, HashAlgorithmName.SHA256);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));

            // Create PFX (PKCS #12) keypair
            File.WriteAllBytes(pfxPath, cert.Export(X509ContentType.Pfx));
            // Create certificate with public key only
            File.WriteAllBytes(pfxPath + ".cer", cert.Export(X509ContentType.Cert));
        }

    }
}
