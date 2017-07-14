using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace cypher
{
    public class DecryptionFile : IDisposable
    {
        private string _encryptedPath;
        private PasswordEncryptionProvider _cryptProvider;
        private DateTime _origWriteTime; 

        public DecryptionFile(string encryptedPath, string password)
        {
            _cryptProvider = new PasswordEncryptionProvider(password);
            _encryptedPath = encryptedPath;
            var path = Path.Combine(Path.GetTempPath(), Path.GetFileName(encryptedPath) + ".dcrypt");

            //open the encrypted file
            using (var encryptedFile = new FileStream(encryptedPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            //create the temporary decryption file
            using (var decryptedFile = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                DecryptedPath = path;
                _cryptProvider.DecryptFileAsync(encryptedFile, decryptedFile, CancellationToken.None).GetAwaiter().GetResult();
            }
            _origWriteTime = File.GetLastWriteTimeUtc(DecryptedPath);
        }
   
        public string DecryptedPath { get; private set; }

        public void Dispose()
        {
            if (File.GetLastWriteTimeUtc(DecryptedPath) > _origWriteTime)
            {
                //open the encrypted file
                using (var encryptedFile = new FileStream(_encryptedPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                //create the temporary decryption file
                using (var decryptedFile = new FileStream(DecryptedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 512))
                {
                    _cryptProvider.EncryptFileAsync(encryptedFile, decryptedFile, CancellationToken.None).GetAwaiter().GetResult();
                }
            }

            File.Delete(DecryptedPath);
        }
    }
}
