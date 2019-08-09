# Azure.Security.KeyVault.Keys.Cryptography

The `Azure.Security.KeyVault.Keys.Cryptography` namespace enables developers to perform cryptographic operations using keys stored in Azure Key Vault. It enables both primitive cryptographic operations such as `encrypt`, `decrypt`, `sign`, `verify`, `wrap`, and `unwrap`, as well as higher order data protection APIs.

## CryptographyClient

The `CryptographyClient` enables users to perform cryptographic operations through the Key Vault service.  It is a low level client in that it only exposes cryptographic primitives.

~~~ c#
public class CryptographyClient
{
    // constructors

    protected CryptographyClient();
    public CryptographyClient(Uri keyId, TokenCredential credential);
    public CryptographyClient(Uri keyId, TokenCredential credential, CryptographyClientOptions options);

    // encrypt

    public virtual async Task<Response<EncryptResult>> EncryptAsync(EncryptionAlgorithm algorithm, byte[] plaintext, CancellationToken cancellationToken = default);
    public virtual async Task<Response<EncryptResult>> EncryptAsync(EncryptionAlgorithm algorithm, byte[] plaintext, byte[] iv, byte[] authenticationData = default, CancellationToken cancellationToken = default);
    public virtual Response<EncryptResult> Encrypt(EncryptionAlgorithm algorithm, byte[] plaintext);
    public virtual Response<EncryptResult> Encrypt(EncryptionAlgorithm algorithm, byte[] plaintext, byte[] iv, byte[] authenticationData = default, CancellationToken cancellationToken = default);

    // decrypt

    public virtual async Task<Response<DecryptResult>> DecryptAsync(EncryptionAlgorithm algorithm, byte[] ciphertext, CancellationToken cancellationToken = default);
    public virtual async Task<Response<DecryptResult>> DecryptAsync(EncryptionAlgorithm algorithm, byte[] ciphertext, byte[] iv, byte[] authenticationData = default, byte[] authenticationTag = default, CancellationToken cancellationToken = default);
    public virtual Response<DecryptResult> Decrypt(EncryptionAlgorithm algorithm, byte[] ciphertext, CancellationToken cancellationToken = default);
    public virtual Response<DecryptResult> Decrypt(EncryptionAlgorithm algorithm, byte[] ciphertext, byte[] iv, byte[] authenticationData = default, byte[] authenticationTag = default, CancellationToken cancellationToken = default);
   
    // sign

    public virtual async Task<Response<SignResult>> SignAsync(SignatureAlgorithm algorithm, byte[] digest, CancellationToken cancellationToken = default);
    public virtual Response<SignResult> Sign(SignatureAlgorithm algorithm, byte[] digest, CancellationToken cancellationToken = default);

    // verify

    public virtual async Task<Response<VerifyResult>> VerifyAsync(SignatureAlgorithm algorithm, byte[] digest, byte[] signature, CancellationToken cancellationToken = default);
    public virtual Response<VerifyResult> Verify(SignatureAlgorithm algorithm, byte[] digest, byte[] signature, CancellationToken cancellationToken = default);
    
    // wrap

    public virtual async Task<Response<WrapResult>> WrapKeyAsync(KeyWrapAlgorithm algorithm, byte[] key, CancellationToken cancellationToken = default);
    public virtual Response<WrapResult> WrapKey(KeyWrapAlgorithm algorithm, byte[] key, CancellationToken cancellationToken = default);
    
    // unwrap

    public virtual async Task<Response<UnwrapResult>> UnwrapKeyAsync(KeyWrapAlgorithm algorithm, byte[] encryptedKey, CancellationToken cancellationToken = default);
    public virtual Response<UnwrapResult> UnwrapKey(KeyWrapAlgorithm algorithm, byte[] encryptedKey, CancellationToken cancellationToken = default);

}
~~~

### Example Usage

~~~ c#
var keyClient = new KeyClient(new Uri("http://myvault.azure.vault.net/"), new DefaultAzureCredential());

Key key = await client.CreateKeyAsync(keyName, KeyType.RSA);

var cryptoClient = new CryptographyClient(key.KeyId, new DefaultAzureCredential());

byte[] data = Encoding.Unicode.GetBytes("Random data to encrypt and sign");

// encrypt and decrypt

EncryptResult encrypted = await cryptoClient.EncryptAsync(EncryptionAlgorithm.RSAOAEP, data);

DecryptResult decrypted = await cryptoClient.DecryptAsync(encrypted.Algorithm, encrypted.Ciphertext);

// sign and verify

// users must hash the data with the correct hash algorithm
using (SHA256 hashAlgo = SHA256.Create())
{
    byte[] digest = hashAlgo.ComputeHash(data);

    SignResult signed = await cryptoClient.SignAsync(SignatureAlgorithm.RS256, digest);

    VerifyResult verified = await cryptoClient.VerifyAsync(signed.Algorithm, digest, signed.Signature);   
}

// wrap and unwrap
using(RandomNumberGenerator rng = new RNGCryptoServiceProvider())
{
    byte[] keyToWrap = new byte[32];
    
    rng.GetBytes(keyToWrap);

    WrapResult wrapped = await cryptoClient.WrapKeyAsync(KeyWrapAlgorithm.RSAOAEP, keyToWrap);

    UnwrapResult unwrapped = await cryptoClient.UnwrapKeyAsync(wrapped.Algorithm, wrapped.EncryptedKey);
}
~~~

## CryptographicKey

The `CryptographicKey` is an abstraction of the Key Vault cryptographic service methods. It provides optimizations on top of the `CryptographyClient` because the `CryptographicKey` class caches the key material locally and __can__ use it to perform some operations locally.  Which operations are performed locally depends on the key material which is available locally, as well as the cryptographic capabilities of the platform.  

In addition to providing some client side optimization the `CryptographicKey` also provides the convenience methods. For instance it provides overloads of the `Encrypt` and `Decrypt` methods which except a Stream to allow users to conveniently encrypt larger payloads. Another example are the `SignData` and `VerifyData` methods, which eliminates the users need to create a digest and to know the appropriate hash algorithm for the given signature algorithm.

~~~ c#
public class CryptographicKey : IDisposable
{
    // constructors

    protected CryptographicKey();
    public CryptographicKey(CryptographyClient client);
    
    //encrypt

    public virtual async Task<EncryptResult> EncryptAsync(EncryptionAlgorithm algorithm, byte[] plaintext, byte[] iv = default, byte[] authenticationData = default, CancellationToken cancellationToken = default);
    public virtual async Task<EncryptStreamResult> EncryptAsync(EncryptionAlgorithm algorithm, Stream plaintext, byte[] iv = default, byte[] authenticationData = default, CancellationToken cancellationToken = default);
    public virtual EncryptResult Encrypt(EncryptionAlgorithm algorithm, byte[] plaintext, byte[] iv = default, byte[] authenticationData = default, CancellationToken cancellationToken = default);
    public virtual EncryptStreamResult Encrypt(EncryptionAlgorithm algorithm, Stream plaintext, byte[] iv = default, byte[] authenticationData = default, CancellationToken cancellationToken = default);

    // decrypt

    public virtual async Task<DecryptResult> DecryptAsync(EncryptionAlgorithm algorithm, byte[] ciphertext, byte[] iv = default, byte[] authenticationData = default, byte[] authenticationTag = default, CancellationToken cancellationToken = default);
    public virtual async Task<DecryptStreamResult> DecryptAsync(EncryptionAlgorithm algorithm, Stream ciphertext, byte[] iv = default, byte[] authenticationData = default, byte[] authenticationTag = default, CancellationToken cancellationToken = default);
    public virtual DecryptResult Decrypt(EncryptionAlgorithm algorithm, byte[] ciphertext, byte[] iv = default, byte[] authenticationData = default, byte[] authenticationTag = default, CancellationToken cancellationToken = default);
    public virtual DecryptStreamResult Decrypt(EncryptionAlgorithm algorithm, Stream ciphertext, byte[] iv = default, byte[] authenticationData = default, byte[] authenticationTag = default, CancellationToken cancellationToken = default);

    // wrap

    public virtual async Task<WrapResult> WrapKeyAsync(KeyWrapAlgorithm algorithm, byte[] key, CancellationToken cancellationToken = default);
    public virtual WrapResult WrapKey(KeyWrapAlgorithm algorithm, byte[] key, CancellationToken cancellationToken = default);

    // unwrap

    public virtual async Task<UnwrapResult> UnwrapKeyAsync(KeyWrapAlgorithm algorithm, byte[] encryptedKey, CancellationToken cancellationToken = default);
    public virtual UnwrapResult UnwrapKey(KeyWrapAlgorithm algorithm, byte[] encryptedKey, CancellationToken cancellationToken = default);

    // sign

    public virtual async Task<SignResult> SignAsync(SignatureAlgorithm algorithm, byte[] digest, CancellationToken cancellationToken = default);
    public virtual SignResult Sign(SignatureAlgorithm algorithm, byte[] digest, CancellationToken cancellationToken = default);

    // verify

    public virtual async Task<VerifyResult> VerifyAsync(SignatureAlgorithm algorithm, byte[] digest, byte[] signature, CancellationToken cancellationToken = default);
    public virtual VerifyResult Verify(SignatureAlgorithm algorithm, byte[] digest, byte[] signature, CancellationToken cancellationToken = default);

    // sign data

    public virtual async Task<SignResult> SignDataAsync(SignatureAlgorithm algorithm, byte[] data, CancellationToken cancellationToken = default);
    public virtual SignResult SignData(SignatureAlgorithm algorithm, byte[] data, CancellationToken cancellationToken = default);
    public virtual async Task<SignResult> SignDataAsync(SignatureAlgorithm algorithm, Stream data, CancellationToken cancellationToken = default);
    public virtual SignResult SignData(SignatureAlgorithm algorithm, Stream data, CancellationToken cancellationToken = default);

    // verify data

    public virtual async Task<VerifyResult> VerifyDataAsync(SignatureAlgorithm algorithm, byte[] data, byte[] signature, CancellationToken cancellationToken = default);
    public virtual VerifyResult VerifyData(SignatureAlgorithm algorithm, byte[] data, byte[] signature, CancellationToken cancellationToken = default);
    public virtual async Task<VerifyResult> VerifyDataAsync(SignatureAlgorithm algorithm, Stream data, byte[] signature, CancellationToken cancellationToken = default);
    public virtual VerifyResult VerifyData(SignatureAlgorithm algorithm, Stream data, byte[] signature, CancellationToken cancellationToken = default);

    public void Dispose();
}
~~~

Also to aid in the creation of a `CryptographicKey` extensions methods to the `KeyClient` class are exposed when using the `Azure.Security.KeyVault.Keys.Cryptography` namespace.

~~~ c#
public partial class KeyClient
{

    public virtual Response<CrytpographicKey> CreateCryptographicKey(string name, KeyType keyType, KeyCreateOptions keyOptions = default, CancellationToken cancellationToken = default);
    public virtual async Task<Response<CryptographicKey>> CreateCryptographicKeyAsync(string name, KeyType keyType, KeyCreateOptions keyOptions = default, CancellationToken cancellationToken = default);

    public virtual Response<CryptographicKey> CreateEcCryptographicKey(EcKeyCreateOptions ecKey, CancellationToken cancellationToken = default);
    public virtual async Task<Response<CryptographicKey>> CreateEcKeyCryptographicAsync(EcKeyCreateOptions ecKey, CancellationToken cancellationToken = default);

    public virtual Response<CryptographicKey> CreateRsaCryptographicKey(RsaKeyCreateOptions rsaKey, CancellationToken cancellationToken = default);
    public virtual async Task<Response<Key>> CreateRsaCryptographicKeyAsync(RsaKeyCreateOptions rsaKey, CancellationToken cancellationToken = default);

    public virtual CryptographicKey GetCryptographicKey(string keyName, string version = null, CancellationToken cancellationToken = default);
    public virtual async Task<CryptographicKey> GetCryptographicKeyAysnc(string keyName, string version = null, CancellationToken cancellationToken = default);
}
~~~

### Example Usage

~~~ c#
var keyClient = new KeyClient(new Uri("http://myvault.azure.vault.net/"), new DefaultAzureCredential());

// encrypt decrypt with stream

CryptographicKey key = await client.CreateCryptographicKeyAsync(keyName, KeyType.AES);

using (FileStream plaintextFile = File.OpenRead("plaintext.bin"))
using (FileStream encryptedFile = File.OpenWrite("encrypted.bin"))
{
    EncryptStreamResult encrypted = await key.EncryptAsync(EncryptionAlgorithm.AESCBC, plaintextFile);

    await encrypted.CiphertextStream.CopyToAsync(encryptedFile);
}

using (FileStream plaintextFile = File.OpenWrite("plaintext.bin"))
using (FileStream encryptedFile = File.OpenRead("encrypted.bin"))
{
    DecryptStreamResult decrypted = await key.DecryptAsync(encrypted.Algorithm, encryptedFile, encrypted.Iv);

    await decrypted.PlaintextStream.CopyToAsync(plaintextFile);
}

// sign data and verify data

CryptographicKey key = await client.CreateCryptographicKeyAsync(keyName, KeyType.EC);

byte[] data = Encoding.Unicode.GetBytes("Random data to encrypt and sign");

SignResult signed = await cryptoClient.SignAsync(SignatureAlgorithm.ES256, data);

VerifyResult verified = await cryptoClient.VerifyAsync(signed.Algorithm, data, signed.Signature);   
~~~

## DataProtectionClient

The `DataProtectionClient` has the highest level API in the `Azure.Security.KeyVault.Keys.Cryptography` namespace. It provides the ability to `protect`, `unprotect`, `sign` and `verify` data
without the user having to have any knowledge of the algorithms being used or their required input. It will automatically choose the best suited algorithm based off the available key, and automatically
create key hierarchies so data can be re-protected without decrypting. It uses the compact JWS and JWE format so the encryption and signatures are inherently portable.
~~~ c#
public class DataProtectionClient
{
    protected DataProtectionClient();
    public DataProtectionClient(TokenCredential credential);
    public DataProtectionClient(TokenCredential credential, DataProtectionClientOptions options);

    public async Task<string> ProtectAsync(Uri keyId, byte[] data, CancellationToken cancellationToken = default);
    public async Task<Stream> ProtectAsync(Uri keyId, Stream data, CancellationToken cancellationToken = default);
    public string Protect(Uri keyId, byte[] data, CancellationToken cancellationToken = default);
    public Stream Protect(Uri keyId, Stream data, CancellationToken cancellationToken = default);

    public async Task<byte[]> UnprotectAsync(string jwe, CancellationToken cancellationToken = default);
    public async Task<Stream> UnprotectAsync(Stream jwe, CancellationToken cancellationToken = default);
    public byte[] Unprotect(string jwe, CancellationToken cancellationToken = default);
    public Stream Unprotect(Stream jwe, CancellationToken cancellationToken = default);

    public async Task<string> SignAsync(Uri keyId, byte[] data, CancellationToken cancellationToken = default);
    public async Task<string> SignAsync(Uri keyId, Stream data, CancellationToken cancellationToken = default);
    public string Sign(Uri keyId, byte[] data, CancellationToken cancellationToken = default);
    public string Sign(Uri keyId, Stream data, CancellationToken cancellationToken = default);

    public async Task<bool> VerifyAsync(string jws, CancellationToken cancellationToken = default);
    public async Task<bool> SignAsync(Stream jwsStream, CancellationToken cancellationToken = default);
    public bool Verify(string jws, CancellationToken cancellationToken = default);
    public bool Verify(Stream jwsStream, CancellationToken cancellationToken = default);
}
~~~

### Expected Usage
~~~ c#
var keyClient = new KeyClient(new Uri("http://myvault.azure.vault.net/"), new DefaultAzureCredential());

Key key = await client.CreateKeyAsync(keyName, KeyType.RSA);

var dpClient = new DataProtectionClient(new DefaultAzureCredential());

byte[] data = Encoding.Unicode.GetBytes("Random data to encrypt and sign");

// protect unprotect

string jwe = await dpClient.ProtectAsync(key.Id, data);

byte[] unprotected = await dpClient.UnprotectAsync(jwe);

// sign and verify

string jws = await dpClient.SignAsync(key.Id, data);

bool valid = await dpClient.verifyAsync(jws);
~~~