using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Common.Cipher
{
    /// <summary>
    /// https://stackoverflow.com/questions/60889345/using-the-aesgcm-class
    /// </summary>
    public class Aes256Gcm : IDisposable
    {
        private AesGcm m_aes;
        private byte[] m_key;

        public Aes256Gcm Copy()
        {
            return new Aes256Gcm(m_key);
        }

        public Aes256Gcm(string password)
        {
            SetPassword(password);
        }
        public Aes256Gcm(byte[] key)
        {
            SetKey(key);
        }

        public void SetPassword(string password)
        {
            byte[] salt = Encoding.ASCII.GetBytes("S1mple3ecuReChaT");
            byte[] key = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256).GetBytes(32);
            SetKey(key);
        }
        public void SetKey(byte[] key)
        {
            if (key == null || key.Length != 32) throw new Exception("Key must be 32 bytes");
            if (m_aes != null) m_aes.Dispose();
            m_key = key;
            m_aes = new AesGcm(key);
        }

        public byte[] Encrypt(byte[] plain)
        {
            // Get bytes of plaintext string
            byte[] plainBytes = plain;

            // Get parameter sizes
            int nonceSize = AesGcm.NonceByteSizes.MaxSize;
            int tagSize = AesGcm.TagByteSizes.MaxSize;
            int cipherSize = plainBytes.Length;

            // We write everything into one big array for easier encoding
            int encryptedDataLength = 4 + nonceSize + 4 + tagSize + cipherSize;
            Span<byte> encryptedData = encryptedDataLength < 1024 ? stackalloc byte[encryptedDataLength] : new byte[encryptedDataLength].AsSpan();

            // Copy parameters
            BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(0, 4), nonceSize);
            BinaryPrimitives.WriteInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4), tagSize);
            var nonce = encryptedData.Slice(4, nonceSize);
            var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
            var cipherBytes = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

            // Generate secure nonce
            RandomNumberGenerator.Fill(nonce);

            // Encrypt
            m_aes.Encrypt(nonce, plainBytes.AsSpan(), cipherBytes, tag);

            // Encode for transmission
            return encryptedData.ToArray();
        }

        public byte[] Decrypt(byte[] cipher)
        {
            // Decode
            Span<byte> encryptedData = cipher;

            // Extract parameter sizes
            int nonceSize = BinaryPrimitives.ReadInt32LittleEndian(encryptedData.Slice(0, 4));
            int tagSize = BinaryPrimitives.ReadInt32LittleEndian(encryptedData.Slice(4 + nonceSize, 4));
            int cipherSize = encryptedData.Length - 4 - nonceSize - 4 - tagSize;

            // Extract parameters
            var nonce = encryptedData.Slice(4, nonceSize);
            var tag = encryptedData.Slice(4 + nonceSize + 4, tagSize);
            var cipherBytes = encryptedData.Slice(4 + nonceSize + 4 + tagSize, cipherSize);

            // Decrypt
            Span<byte> plainBytes = cipherSize < 1024 ? stackalloc byte[cipherSize] : new byte[cipherSize];
            m_aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            // Convert plain bytes back into string
            return plainBytes.ToArray();
        }
        public void Dispose()
        {
            m_key = Array.Empty<byte>();
            m_aes.Dispose();
        }
    }
}
