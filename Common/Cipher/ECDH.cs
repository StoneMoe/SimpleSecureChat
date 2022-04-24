using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Common.Cipher
{
    /// <summary>
    /// ECC 512bit Diffie Hellman
    /// </summary>
    public class ECDH
    {
        readonly ECDiffieHellmanCng local;

        /// <summary>
        /// Init ECDH with random key
        /// </summary>
        public ECDH()
        {
            local = new(keySize: 521);
            local.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            local.HashAlgorithm = CngAlgorithm.Sha256;
            local.SecretPrepend = Encoding.UTF8.GetBytes("SSCv3");
            local.SecretAppend = Encoding.UTF8.GetBytes("ECDH");
        }

        /// <summary>
        /// Init ECDH from existed ECDSA X.509 Certificate
        /// </summary>
        /// <param name="cert"></param>
        /// <exception cref="Exception"></exception>
        public ECDH(X509Certificate2 cert)
        {
            ECDsaCng? privKey = cert.GetECDsaPrivateKey() as ECDsaCng;
            if (privKey == null)
                throw new Exception("Invalid Certificate");
            if (privKey.Key.KeySize != 521)
                throw new Exception("Unmatch key size");

            // make it exportable
            privKey.Key.SetProperty(new CngProperty("Export Policy", BitConverter.GetBytes((int)CngExportPolicies.AllowPlaintextExport), CngPropertyOptions.Persist));

            var final = privKey.Key.Export(CngKeyBlobFormat.EccPrivateBlob);
            final[2] = 0x4b;  // patch header from BCRYPT_ECDSA_PRIVATE_P521_MAGIC to BCRYPT_ECDH_PRIVATE_P521_MAGIC

            local = new(CngKey.Import(final, CngKeyBlobFormat.EccPrivateBlob));
            local.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            local.HashAlgorithm = CngAlgorithm.Sha256;
            local.SecretPrepend = Encoding.UTF8.GetBytes("SSCv3");
            local.SecretAppend = Encoding.UTF8.GetBytes("ECDH");
        }

        public byte[] DeriveKey(X509Certificate2 remoteCert)
        {
            ECDsaCng? publicKey = remoteCert.GetECDsaPublicKey() as ECDsaCng;
            if (publicKey == null)
                throw new Exception("Invalid certificate");

            ECParameters remotePublicParams = publicKey.ExportParameters(false);
            ECDiffieHellman remotePublicCng = ECDiffieHellman.Create(remotePublicParams);
            return local.DeriveKeyMaterial(remotePublicCng.PublicKey);
        }

        public byte[] DeriveKey(byte[] eccPubKeyBlob)
        {
            return local.DeriveKeyMaterial(CngKey.Import(eccPubKeyBlob, CngKeyBlobFormat.EccPublicBlob));
        }

        public byte[] GetPublicKeyBlob()
        {
            return local.PublicKey.ToByteArray();
        }

    }
}
