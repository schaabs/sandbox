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
            
            var rc2 = new RC2CryptoServiceProvider();
            var key = bpwd.CryptDeriveKey("RC2", "SHA1", 128, new byte[rc2.IV.Length] );
            
            var hashProvider = SHA256.Create();

            HeaderCryptoProvider.Key = key;

            PasswordKeyHash = hashProvider.ComputeHash(HeaderCryptoProvider.Key);

            HeaderCryptoProvider.Padding = PaddingMode.Zeros;
            
        }

        public byte[] PasswordKeyHash { get; private set; }

        public AesCryptoServiceProvider HeaderCryptoProvider { get; private set; }

        public AesCryptoServiceProvider ContentCryptoProvder { get; private set; }

        public async Task EncryptFileAsync(string path, CancellationToken cancel)
        {
            using (var writeHandle = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.Read))
            using (var readHandle = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Write))
            {
                byte[] filebuff = GetEncryptedHeaderBytes();

                long bytesWritten = 0;

                var encryptor = ContentCryptoProvder.CreateEncryptor();

                var inblockbuff = new byte[encryptor.InputBlockSize];
                var outblockbuff = new byte[encryptor.OutputBlockSize];

                var bytesRead = 0;

                while((bytesRead = await readHandle.ReadAsync(inblockbuff, 0, inblockbuff.Length)) == inblockbuff.Length)
                {
                    encryptor.TransformBlock(inblockbuff, 0, inblockbuff.Length, outblockbuff, 0);

                    for(int i = 0; i < outblockbuff.Length; i++)
                    {
                        writeHandle.WriteByte(filebuff[bytesWritten % filebuff.Length]);

                        filebuff[bytesWritten % filebuff.Length] = outblockbuff[i];

                        bytesWritten++;
                    }
                }

                outblockbuff = encryptor.TransformFinalBlock(inblockbuff, 0, bytesRead);

                for (int i = 0; i < outblockbuff.Length; i++)
                {
                    writeHandle.WriteByte(filebuff[bytesWritten % filebuff.Length]);

                    filebuff[bytesWritten % filebuff.Length] = outblockbuff[i];

                    bytesWritten++;
                }

                writeHandle.Flush();
            }
        }

        public async Task DecryptFileAsync(string path, CancellationToken cancel)
        {
            using (var writeHandle = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.Read))
            using (var readHandle = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Write))
            {
                await DecryptHeaderAsync(readHandle, cancel);

                var encryptor = ContentCryptoProvder.CreateDecryptor();

                var inblockbuff = new byte[encryptor.InputBlockSize];
                var outblockbuff = new byte[encryptor.OutputBlockSize];

                var contentBytes = 0;
                var bytesRead = 0;

                while ((bytesRead = await readHandle.ReadAsync(inblockbuff, 0, inblockbuff.Length)) == inblockbuff.Length)
                {
                    encryptor.TransformBlock(inblockbuff, 0, inblockbuff.Length, outblockbuff, 0);

                    await writeHandle.WriteAsync(outblockbuff, 0, outblockbuff.Length);

                    contentBytes += outblockbuff.Length;
                }

                outblockbuff = encryptor.TransformFinalBlock(inblockbuff, 0, bytesRead);

                contentBytes += bytesRead;
                
                await writeHandle.WriteAsync(outblockbuff, 0, outblockbuff.Length);

                contentBytes += outblockbuff.Length;

                writeHandle.Flush();

                writeHandle.SetLength(contentBytes);
            }
        }

        public async Task RecryptFileAsync(string path, string password, CancellationToken cancel)
        {
            using (var writeHandle = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.Read))
            using (var readHandle = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Write))
            {
                await DecryptHeaderAsync(readHandle, cancel);

                InitializeHeaderCryptoProvider(password);

                byte[] newHeader = GetEncryptedHeaderBytes();

                await writeHandle.WriteAsync(newHeader, 0, newHeader.Length);

                await writeHandle.FlushAsync();
            }
        }

        private async Task DecryptHeaderAsync(Stream input, CancellationToken cancel)
        {
            //read the hash and make sure it matches the password hash
            for (int i = 0; i < PasswordKeyHash.Length; i++)
            {
                if (input.ReadByte() != PasswordKeyHash[i])
                {
                    throw new InvalidPasswordException();
                }
            }

            //get the IV used to encrypt the content key / IV
            var headerIV = new byte[HeaderCryptoProvider.IV.Length];

            await input.ReadAsync(headerIV, 0, headerIV.Length);

            HeaderCryptoProvider.IV = headerIV;

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

        private byte[] GetEncryptedHeaderBytes()
        {
            var contentKeyBytes = GetClearHeader();

            var encryptedContentKeyBytes = new byte[PasswordKeyHash.Length + HeaderCryptoProvider.IV.Length + contentKeyBytes.Length];

            using (MemoryStream buffStream = new MemoryStream(encryptedContentKeyBytes))
            {
                //write the password hash to the buffer for password validation
                buffStream.Write(PasswordKeyHash, 0, PasswordKeyHash.Length);

                //write the header IV to the buffer for header decryption
                buffStream.Write(HeaderCryptoProvider.IV, 0, HeaderCryptoProvider.IV.Length);

                using (CryptoStream csEncrypt = new CryptoStream(buffStream, HeaderCryptoProvider.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    csEncrypt.Write(contentKeyBytes, 0, contentKeyBytes.Length);
                }
            }

            return encryptedContentKeyBytes;
        }

        private byte[] GetClearHeader()
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
