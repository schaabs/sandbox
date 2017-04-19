using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace filecrypto
{
    public class InvalidPasswordException : Exception
    {

    }

    public class PasswordEncryptionProvider
    {
        // Create a new RNGCryptoServiceProvider.
        private RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();

        private const int HEADER_LENGTH = 256;

        public PasswordEncryptionProvider(string password)
        {
            HeaderCryptoProvider = new AesCryptoServiceProvider();

            HeaderCryptoProvider.Padding = PaddingMode.Zeros;

            InitializeHeaderCryptoProvider(password);

            ContentCryptoProvder = new AesCryptoServiceProvider();

            ContentCryptoProvder.Padding = PaddingMode.Zeros;
        }

        private void InitializeHeaderCryptoProvider(string password)
        {
            var bpwd = new PasswordDeriveBytes(password, CreateRandomSalt(7));

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();

            var key_init = bpwd.CryptDeriveKey("TripleDES", "SHA512", 192, tdes.IV);

            var hashProvider = SHA256.Create();

            HeaderCryptoProvider.Key = hashProvider.ComputeHash(key_init);

            PasswordKeyHash = hashProvider.ComputeHash(HeaderCryptoProvider.Key);

            HeaderCryptoProvider.Padding = PaddingMode.Zeros;

            byte[] iv = hashProvider.ComputeHash(PasswordKeyHash);

            Array.Resize(ref iv, HeaderCryptoProvider.BlockSize / 8);

            HeaderCryptoProvider.IV = iv;
        }

        public byte[] PasswordKeyHash { get; private set; }

        public AesCryptoServiceProvider HeaderCryptoProvider { get; private set; }

        public AesCryptoServiceProvider ContentCryptoProvder { get; private set; }

        public async Task EncryptToStreamAsync(Stream input, Stream output, CancellationToken cancel)
        {
            await WriteHeaderToStream(output, cancel);

            await WriteContentToStreamAsync(input, output, cancel);
        }

        public async Task DecryptFromStreamAsync(Stream input, Stream output, CancellationToken cancel)
        {
            await DecryptHeader(input, cancel);

            await DecryptContentFromStreamAsync(input, output, cancel);
        }

        public async Task RecryptStreamAsync(string password, Stream stream, CancellationToken cancel)
        {
            await DecryptHeader(stream, cancel);

            InitializeHeaderCryptoProvider(password);

            stream.Seek(0, SeekOrigin.Begin);

            await WriteHeaderToStream(stream, cancel);
        }

        private async Task DecryptContentFromStreamAsync(Stream input, Stream output, CancellationToken cancel)
        {
            using (CryptoStream csDecrypt = new CryptoStream(input, ContentCryptoProvder.CreateDecryptor(), CryptoStreamMode.Read))
            {
                await csDecrypt.CopyToAsync(output);

                await output.FlushAsync();
            }
        }

        private async Task DecryptHeader(Stream input, CancellationToken cancel)
        {
            //read the hash and make sure it matches the password hash
            for (int i = 0; i < PasswordKeyHash.Length; i++)
            {
                if (input.ReadByte() != PasswordKeyHash[i])
                {
                    throw new InvalidPasswordException();
                }
            }

            //if the hash matches read the encrypted header
            var header = new byte[HEADER_LENGTH];

            await input.ReadAsync(header, 0, HEADER_LENGTH);

            SetContentKeyFromHeader(header);

        }

        private void SetContentKeyFromHeader(byte[] headerBytes)
        {
            var decryptedBytes = new byte[headerBytes.Length];

            using (MemoryStream inStream = new MemoryStream(headerBytes))
            {
                using (CryptoStream decryptStream = new CryptoStream(inStream, HeaderCryptoProvider.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    if (decryptStream.Read(decryptedBytes, 0, decryptedBytes.Length) == decryptedBytes.Length)
                    {
                        var key = new byte[ContentCryptoProvder.Key.Length];

                        var iv = new byte[ContentCryptoProvder.IV.Length];

                        Array.ConstrainedCopy(decryptedBytes, GetKeyHeaderOffset(), key, 0, key.Length);

                        Array.ConstrainedCopy(decryptedBytes, GetIVHeaderOffset(), iv, 0, iv.Length);

                        ContentCryptoProvder.Key = key;

                        ContentCryptoProvder.IV = iv;
                        
                    }
                }
            }
        }

        private async Task WriteHeaderToStream(Stream stream, CancellationToken cancel)
        {
            await stream.WriteAsync(PasswordKeyHash, 0, PasswordKeyHash.Length, cancel);

            var contentKeyBytes = GetHeader();

            var encryptedContentKeyBytes = new byte[contentKeyBytes.Length];

            using (MemoryStream buffStream = new MemoryStream(encryptedContentKeyBytes))
            {
                using (CryptoStream csEncrypt = new CryptoStream(buffStream, HeaderCryptoProvider.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    await csEncrypt.WriteAsync(contentKeyBytes, 0, contentKeyBytes.Length);
                }

                await stream.WriteAsync(encryptedContentKeyBytes, 0, encryptedContentKeyBytes.Length);

                await stream.FlushAsync();
            }
        }

        private byte[] GetHeader()
        {
            var entropy = new byte[GetEntropyLength()];

            rand.GetBytes(entropy);

            return entropy.Concat(ContentCryptoProvder.Key.Concat(ContentCryptoProvder.IV)).ToArray();
        }

        private int GetKeyHeaderOffset()
        {
            return GetEntropyLength();
        }

        private int GetIVHeaderOffset()
        {
            return GetKeyHeaderOffset() + ContentCryptoProvder.Key.Length;
        }

        private int GetEntropyLength()
        {
            return HEADER_LENGTH - (ContentCryptoProvder.Key.Length + ContentCryptoProvder.IV.Length);
        }

        private async Task WriteContentToStreamAsync(Stream input, Stream output, CancellationToken cancel)
        {
            using (CryptoStream csEncrypt = new CryptoStream(output, ContentCryptoProvder.CreateEncryptor(), CryptoStreamMode.Write))
            {
                await input.CopyToAsync(csEncrypt);

                await csEncrypt.FlushAsync();

            }
        }


        private byte[] CreateRandomSalt(int length)
        {
            // Create a buffer
            byte[] randBytes;

            if (length >= 1)
            {
                randBytes = new byte[length];
            }
            else
            {
                randBytes = new byte[1];
            }

            // Fill the buffer with random bytes.
            rand.GetBytes(randBytes);

            // return the bytes.
            return randBytes;
        }
    }
}
