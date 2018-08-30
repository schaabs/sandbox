
#include <string>
#include <iostream>
#include "akv_pkcs11.h"

#define ASSERT_CKR_OK(rv) if(rv != CKR_OK) { std::cout << "OPERATION FAILED.  RV: " << rv; return rv; }

int main ( int argc, char *argv[] )
{
    
    //
    // initialize the library
    //

    std::cout << "Initializing KeyVault Cryptoki Library with C_Initialize";

    // our implementation requires the following:
    //   Mutex callbacks are not specified
    //   OS locking is allowed  (CKF_OS_LOCKING_OK must be set in flags)
    //   Creating threads is allowed (CKF_LIBRARY_CANT_CREATE_OS_THREADS must NOT be set in flags)
    CK_C_INITIALIZE_ARGS initArgs{
        NULL_PTR,             // CK_CREATEMUTEX CreateMutex
        NULL_PTR,             // CK_DESTROYMUTEX DestroyMutex
        NULL_PTR,             // CK_LOCKMUTEX LockMutex
        NULL_PTR,             // CK_UNLOCKMUTEX UnlockMutex
        CKF_OS_LOCKING_OK,    // CK_FLAGS flags  
        NULL                  // CK_VOID_PTR pReserved
    };

    CK_RV rv = C_Initialize(&initArgs);

    // Assert C_Initialize succeeded
    ASSERT_CKR_OK(rv);

    //
    // open the session
    //

    std::cout << "Creating a new session with C_OpenSession";

    // our implemenation currently does not support notification callbacks so Notify must be NULL_PTR
    CK_SESSION_HANDLE hSession;

    rv =  C_OpenSession( 0,                                    //  CK_SLOT_ID slotID,
                         CKF_RW_SESSION | CKF_SERIAL_SESSION,  //  CK_FLAGS flags,
                         NULL_PTR,                             //  CK_VOID_PTR pApplication,
                         NULL_PTR,                             //  CK_NOTIFY Notify,
                         &hSession);                           //  CK_SESSION_HANDLE_PTR phSession

    // Assert C_OpenSession succeeded
    ASSERT_CKR_OK(rv);
    
    //
    // login
    //

    std::cout << "Setting up session authentication with C_Login";

    // if MSI authentication is supported on the machine pPin should be NULL_PTR 
    // to login with clientid and client secret pPin should be a utf8 string in the format:
    //      clientid=<clientid>;secret=<clientsecret>
    rv = C_Login(hSession,           // CK_SESSION_HANDLE hSession
                 CKU_USER,           // CK_USER_TYPE userType (currently ignored)
                 NULL_PTR,           // CK_UTF8CHAR_PTR pPin 
                 0);                 // CK_ULONG ulPinLen
 
    // Assert C_Login succeeded
    ASSERT_CKR_OK(rv);

    //
    // generate a new key pair
    //

    std::cout << "Generating a RSA Key Pair with C_GenerateKeyPair";

    // the only required attribute type for generating a key is CKA_ID
    // the value of CKA_ID should be the keyvault kid, i.e. the full uri to the key as it
    // will exist in the vault
    std::string kid("https://pkcs11test.vault.azure.net/keys/key1");

    CK_ATTRIBUTE keyTemplate[] {
        {CKA_ID, &kidUtf8[0], kidUtf8.size()}
    };

    // currently only RSA keys are supported so the only supported mechanim is CKM_RSA_PKCS_KEY_PAIR_GEN
    CK_MECHANISM genMech = { CKM_RSA_PKCS_KEY_PAIR_GEN, NULL_PTR, 0 };
    
    CK_OBJECT_HANDLE hPublicKey, hPrivateKey;
    
    rv = C_GenerateKeyPair(hSession,            // CK_SESSION_HANDLE hSession
                           &genMech,            // CK_MECHANISM_PTR pMechanism
                           keyTemplate,         // CK_ATTRIBUTE_PTR pPublicKeyTemplate
                           1,                   // CK_ULONG ulPublicKeyAttributeCount
                           keyTemplate,         // CK_ATTRIBUTE_PTR pPrivateKeyTemplate
                           1,                   // CK_ULONG ulPrivateKeyAttributeCount
                           &hPublicKey,         // CK_OBJECT_HANDLE_PTR phPublicKey
                           &hPrivateKey);       // CK_OBJECT_HANDLE_PTR phPrivateKey
    
    // Assert C_GenerateKeyPair succeeded
    ASSERT_CKR_OK(rv);
    
    CK_BYTE data[] = { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
    
    CK_BYTE buff[512];

    CK_ULONG buffsize = sizeof(buff);
    
    //
    // encrypt with the public key
    //

    std::cout << "Initializing encryption operation with C_EncryptInit";

    // currently the only supported encryption mechanim is CKM_RSA_PKCS_OAEP
    CK_MECHANISM cryptMech = { CKM_RSA_PKCS_OAEP, NULL_PTR, 0 };
    
    // initialize the encrypt operation
    rv = C_EncryptInit(hSession,          // CK_SESSION_HANDLE hSession
                       &cryptMech,        // CK_MECHANISM_PTR pMechanism
                       hPublicKey);       // CK_OBJECT_HANDLE hKey
    
    // Assert C_EncryptInit succeeded
    ASSERT_CKR_OK(rv);
    

    std::cout << "Encrypting data with C_Encrypt";

    // encrypt data
    rv = C_Encrypt(hSession,          // CK_SESSION_HANDLE hSession
                   &data[0],             // CK_BYTE_PTR pData,
                   sizeof(data),      // CK_ULONG ulDataLen,
                   &buff[0],             // CK_BYTE_PTR pEncryptedData,
                   &buffsize);        // CK_ULONG_PTR pulEncryptedDataLen

    // Assert C_Encrypt succeeded
    ASSERT_CKR_OK(rv);

    CK_ULONG decryptbuffsize = sizeof(buff);
    
    //
    // decrypt with the private key
    //

    std::cout << "Initializing decryption operation with C_DecryptInit";

    // initialize the decryption operation
    rv = C_DecryptInit(hSession,          // CK_SESSION_HANDLE hSession
                       &cryptMech,        // CK_MECHANISM_PTR pMechanism
                       hPrivateKey);      // CK_OBJECT_HANDLE hKey

    // Assert C_DecryptInit succeeded
    ASSERT_CKR_OK(rv);


    std::cout << "Decrypting data with C_Decrypt";

    // decrypt data
    rv = C_Decrypt(hSession,          // CK_SESSION_HANDLE hSession
                   &buff[0],             // CK_BYTE_PTR pData,
                   buffsize,          // CK_ULONG ulDataLen,
                   &buff[0],             // CK_BYTE_PTR pEncryptedData,
                   &decryptbuffsize); // CK_ULONG_PTR pulEncryptedDataLen
                       
    
    // Assert C_Decrypt succeeded
    ASSERT_CKR_OK(rv);


    //
    // log out close the session and finalize the library
    //

    std::cout << "Logging out session with C_Logout";

    rv = C_Logout(hSession);
    
    // Assert C_Logout succeeded
    ASSERT_CKR_OK(rv);

    std::cout << "Closing session with C_CloseSession";

    rv = C_CloseSession(hSession);
    
    // Assert C_CloseSession succeeded
    ASSERT_CKR_OK(rv);

    std::cout << "Finalizing keyvault cryptoki library with C_Finalize";

    rv = C_Finalize(NULL_PTR);

    // Assert C_Finalize succeeded
    ASSERT_CKR_OK(rv);


}