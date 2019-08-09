## 1. Client behavior data class
 - Client Options
    - VaultUrl
    - Credentials

## 2. Operational behavior data class
 - TS
    - GetSecretOptions
    - SetSecretOptions
 - Python
    - Kwargs
 - .NET / Java
    - Generally come along with the data object

## 3. Secret / Secret Base / SecretAttributes
 - Read Only (Service Generated)
    - Id
    - Version
    - Managed
    - KeyId
    - Created
    - Updated
    - RecoveryLevel
 - Read Only (Client Specified)
    - Vault
    - Name
    - Value
 - Read / Write
    - NotBefore
    - Expires
    - Enabled
    - ContentType
    - Tags

## Key / KeyBase / KeyAttributes
 - Read Only (Service Generated)
    - Id
    - Version
    - Managed
    - Created
    - Updated
    - RecoveryLevel
 - Read Only (Client Specified)
    - Vault
    - Name
    - KeyMaterial
 - Read / Write
    - NotBefore
    - Expires
    - Enabled
    - ContentType
    - Tags

## Certificate / CertificateBase / CertificateAttributes
 - Read Only (Service Generated)
    - Id
    - Version
    - Thumbprint
    - SecretId
    - KeyId
    - CER
    - NotBefore
    - Expires
    - Created
    - Updated
    - RecoveryLevel
 - Read Only (Client Specified)
    - Vault
    - Name
    - KeyMaterial
 - Read / Write
    - Enabled
    - Tags

## Issuer
- AdminDetails
    - AccountId { get; set; }
    - Password { get; set; }
    - OrganizationId { get; set; }
    - Email { get; set; }
    - FirstName { get; set; }
    - LastName { get; set; }
    - Phone { get; set; }
## Policy
