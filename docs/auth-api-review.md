# __Azure Token Authentication__

## __Types__
__python__
~~~ python
# azure.core.credentials
class TokenCredential(Protocol):

# azure.core.pipeline.policies
class BearerTokenCredentialPolicy(HTTPPolicy):

# azure.identity
class AzureCredential:
class ManagedIdentityCredential:
class ClientSecretCredential:
class ClientCertificateCredential:
class EnvironmentCredential:
class AggregateCredential:
~~~
__javascript__
~~~ typescript
// @azure/core-credentials
export interface TokenCredential { }

// @azure/core-http-policies
export class BearerTokenAuthenticationPolicy extends BaseRequestPolicy;

// @azure/identity
export class AzureCredential;
export class ManagedIdentityCredential implements TokenCredential;
export class ClientSecretCredential implements TokenCredential;
export class ClientCertificateCredential implements TokenCredential;
export class EnvironmentCredential implements TokenCredential;
export class AggregateCredential implements TokenCredential;
export interface IdentityClientOptions extends ServiceClientOptions;
~~~
__java__
~~~ java
// package com.azure.core.credentials;
public abstract class TokenCredential;

// package com.azure.core.http.policy;
public class BearerTokenAuthenticationPolicy extends BaseRequestPolicy;

// package com.azure.identity;
public final class AzureCredential;
public class ManagedIdentityCredential extends TokenCredential;
public class ClientSecretCredential extends TokenCredential;
public class ClientCertificateCredential extends TokenCredential;
public class EnvironmentCredential extends TokenCredential;
public class AggregateCredential extends TokenCredential;
public interface IdentityClientOptions extends ServiceClientOptions;
~~~
__C#__
~~~ c#
// namespace Azure.Core
public abstract class TokenCredential;

// namespace Azure.Core.Pipeline.Policies
public class BearerTokenAuthenticationPolicy : HttpPipelinePolicy;

// namespace Azure.Identity
public static class AzureCredential;
public class ManagedIdentityCredential : TokenCredential;
public class ClientSecretCredential : TokenCredential;
public class ClientCertificateCredential : TokenCredential;
public class EnvironmentCredential : TokenCredential;
public class AggregateCredential : TokenCredential;
public class IdentityClientOptions : HttpClientOptions;
~~~
## __Azure Core__
The following types are exposed from azure core and used by client libraries for token authentication.

### __TokenCredential__
TokenCredential is an abstraction exposed from core which is a basis for azure token authentication across all client libraries.

__python__
~~~ python
class TokenCredential(Protocol):
    def get_token(self, scopes, **kwargs):
        # type: (Iterable[str], Mapping[str, Any]) -> Optional[str]
~~~
__javascript__
~~~ typescript
export interface TokenCredential {
  getToken(
    scopes: string[],
    requestOptions?: RequestOptionsBase
  ): Promise<string | null>; 
}
~~~
__java__
~~~ java
public abstract class TokenCredential {
    public abstract Mono<String> getToken(string[] scopes);
}
~~~
__C#__
~~~ c#
public abstract class TokenCredential
{
    public abstract ValueTask<string> GetTokenAsync(string[] scopes, CancellationToken cancellationToken);

    public abstract string GetToken(string[] scopes, CancellationToken cancellationToken);
}
~~~

### __BearerTokenAuthenticationPolicy__
BearerTokenAuthenticationPolicy is a pipeline policy which authenticates requests. It is constructed with a TokenCredential and the scopes required for authentication.

__python__
~~~ python
class BearerTokenCredentialPolicy(HTTPPolicy):
    def __init__(self, credentials, scopes):
        # type: (str, TokenCredential, Iterable[str]
    def send(self, request, **kwargs):
~~~
__javascript__
~~~ typescript
export class BearerTokenAuthenticationPolicy extends BaseRequestPolicy {
  constructor(
    nextPolicy: RequestPolicy,
    options: RequestPolicyOptions,
    private credential: TokenCredential,
    private scopes: string[]) {
  }

  public async sendRequest(
    webResource: WebResource
  ): Promise<HttpOperationResponse> {
  }
}
~~~
__java__
~~~ java
public class BearerTokenAuthenticationPolicy implements HttpPipelinePolicy {
    public TokenCredentialPolicy(TokenCredential credential, string[] scopes) {
    }

    public Mono<HttpResponse> process(HttpPipelineCallContext context, HttpPipelineNextPolicy next) {
    }
}
~~~
__C#__
~~~ c#
public class BearerTokenAuthenticationPolicy: HttpPipelinePolicy
{
    public BearerTokenAuthenticationPolicy(TokenCredential credential, string scope);

    public BearerTokenAuthenticationPolicy(TokenCredential credential, IEnumerable<string> scopes);

    public override Task ProcessAsync(HttpPipelineMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline);
    
    public override void Process(HttpPipelineMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline);

    public async Task ProcessAsync(HttpPipelineMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline, bool async);
}
~~~

### __Client Consumption__
__python__
~~~ python
class KeyClient:
    def __init__(self, vault_url, credentials, config=None, api_version=None, **kwargs):
        # type: (str, TokenCredential, Optional[Configuration], Optional[str], Mapping[str, Any])
        
        # ...
        policies = [
            # ...
            BearerTokenCredentialPolicy(credentials, ['https://vault.azure.net/.default']),
            # ...
        ]
        # ...
~~~
__javascript__
~~~ typescript

export class KeyClient {
  constructor(
    url: string,
    credential: TokenCredential,
    pipelineOrOptions: Pipeline | NewPipelineOptions = {}) {
  }

  public static getDefaultPipeline(
    credential: TokenCredential,
    pipelineOptions: NewPipelineOptions = {}): Pipeline {
    // ...
    const requestPolicyFactories: RequestPolicyFactory[] = [
      // ...
      authenticationPolicy(credential, [ "https://vault.azure.net/.default" ]);
      // ...
    ];
  }
}
~~~
__java__
~~~ java
public final class KeyAsyncClientBuilder {

    public KeyAsyncClientBuilder credentials(TokenCredential credentials) {
    }
    
    public KeyAsyncClient build() {
        // ...
        policies.add(new TokenCredentialPolicy(credentials, new string[] { "https://vault.azure.net/.default"}));
        // ...
    }
}
~~~
__C#__
~~~ c#
public class KeyClient
{
    public KeyClient(Uri vaultUri, TokenCredential credential)
        : this(vaultUri, credential, null) { } 

    public KeyClient(Uri vaultUri, TokenCredential credential, KeyClientOptions options)
    {
        // ...
        _pipeline = HttpPipeline.Build(options,
                // ...
                new BearerTokenAuthenticationPolicy(credential, "https://vault.azure.net/.default"),
                // ...
                );
        // ...
    }
}
~~~

## __Azure Identity__
Azure identity is a client library which contains implementations of TokenCredential.  Client libaries __DO NOT__ take a dependency  on this library, rather the application developer can choose use this library for convenience.

### __AzureCredential and System Derived Credentials__
The expected common authentication mechanism we want to promote inspects the system for credential data in the following formats.
 - Environment
 - MSI Endpoint
 - Shared Token Cache (out of scope for preview)

The azure identity client library statically exposes a TokenCredential instance to retrieve a credential in this manner.

__python__
~~~ python
class AzureCredential:
    @staticmethod
    def default():
        # type: () -> TokenCredential
~~~
~~~ python
key_client = KeyClient("https://myvault.vault.azure.net/", AzureCredential.default())
~~~
__javascript__
~~~ typescript
export class AzureCredential {
  public static default(): TokenCredential;
}
~~~
~~~ typescript
const client = new KeysClient("https://myvault.vault.azure.net/", AzureCredential.default());
~~~
__java__
~~~ java
public final class AzureCredential {
    public static TokenCredential DEFAULT;
}
~~~
~~~ java
KeyClient keyClient = KeyClient.builder()
                            .endpoint("https://myvault.vault.azure.net/")
                            .credentials(AzureCredential.DEFAULT)
                            .build();
~~~
__C#__
~~~ c#
public static class AzureCredential
{    
    public static TokenCredential Default { get; }
}
~~~
~~~ c#
KeyClient keyClient = new KeyClient("https://myvault.vault.azure.net/", AzureCredential.Default);
~~~

### __ManagedIdentityCredential__
__python__
~~~ python
class ManagedIdentityCredential:
    @staticmethod
    def create_config(**kwargs):
        # type: (Mapping[str, str]) -> Configuration
        # ... 

    def __init__(self, config=None, **kwargs):
        # type: (Optional[Configuration], Mapping[str, Any]) -> None
        
    def get_token(self, scopes, **kwargs):
        # type: (Iterable[str], Mapping[str, Any]) -> str
~~~
~~~ python
key_client = KeyClient("https://myvault.vault.azure.net/", ManagedIdentityCredential())
~~~
__javascript__
~~~ typescript
export class ManagedIdentityCredential implements TokenCredential {
  constructor(options?: IdentityClientOptions);
}
~~~
~~~ typescript
const client = new KeysClient("https://myvault.vault.azure.net/", new ManagedIdentityCredential());
~~~
__java__
~~~ java
public class ManagedIdentityCredential extends TokenCredential {
    public ManagedIdentityCredential();

    public ManagedIdentityCredential(IdentityClientOptions options);
}
~~~
~~~ java
KeyClient keyClient = KeyClient.builder()
                            .endpoint("https://myvault.vault.azure.net/")
                            .credentials(new ManagedIdentityCredential())
                            .build();
~~~
__C#__
~~~ c#
public class ManagedIdentityCredential : TokenCredential
{
    public ManagedIdentityCredential();

    public ManagedIdentityCredential(IdentityClientOptions options);
}
~~~
~~~ c#
KeyClient keyClient = new KeyClient("https://myvault.vault.azure.net/", new ManagedIdentityCredential());
~~~

### __ClientSecretCredential__
__python__
~~~ python
class ClientSecretCredential:
    @staticmethod
    def create_config(**kwargs):
        # type: (Mapping[str, str]) -> Configuration
        # ... 

    def __init__(self, tenant_id, client_id, client_secret, config=None, **kwargs):
        # type: (str, str, str, Optional[Configuration], Mapping[str, Any]) -> None
        
    def get_token(self, scopes, **kwargs):
        # type: (Iterable[str], Mapping[str, Any]) -> str
~~~
~~~ python
credential = ClientSecretCredential(tenant_id=from_custom_config('tenant_id'),
                                    client_id=from_custom_config('client_id'),
                                    client_secret=from_custom_config('client_secret'))
                                    
key_client = KeyClient("https://myvault.vault.azure.net/", credential)
~~~
__javascript__
~~~ typescript
export class ClientSecretCredential implements TokenCredential {
  constructor(
    private tenantId: string,
    private clientId: string,
    private clientSecret: string,
    options?: IdentityClientOptions) {
  }
}
~~~
~~~ typescript
const credential = new ClientSecretCredential(
    fromCustomConfig('tenantId'),
    fromCustomConfig('clientId'),
    fromCustomConfig('clientSecret'));
                                    
const keyClient = new KeyClient("https://myvault.vault.azure.net/", credential);
~~~
__java__
~~~ java
public class ClientSecretCredential extends TokenCredential {
    public ClientSecretCredential(String tenantId, String clientId, String clientSecret);

    public ClientSecretCredential(String tenantId, String clientId, String clientSecret, IdentityClientOptions options);
}
~~~
~~~ java
ClientSecretCredential credential = new ClientSecretCredential(
    fromCustomConfig('tenantId'),
    fromCustomConfig('clientId'),
    fromCustomConfig('clientSecret'));
                                    
KeyClient keyClient = new KeyClient("https://myvault.vault.azure.net/", credential);
~~~
__C#__
~~~ c#
public class ClientSecretCredential : TokenCredential
{
    public ClientSecretCredential(string tenantId, string clientId, string clientSecret);

    public ClientSecretCredential(string tenantId, string clientId, string clientSecret, IdentityClientOptions options);
}
~~~
~~~ c#
ClientSecretCredential credential = new ClientSecretCredential
(
    FromCustomConfig('tenantId'), 
    FromCustomConfig('clientId'),
    FromCustomConfig('clientSecret')
);
                                    
KeyClient keyClient = new KeyClient("https://myvault.vault.azure.net/", credential);
~~~

### __ClientCertificateCredential__
__python__
~~~ python
class CertificateCredential:
    @staticmethod
    def create_config(**kwargs):
        # type: (Mapping[str, str]) -> Configuration
        # ... 

    def __init__(self, tenant_id, client_id, certificate_path, config=None, **kwargs):
        # type: (str, str, str, Optional[Configuration], Mapping[str, Any]) -> None
        
    def get_token(self, scopes, **kwargs):
        # type: (Iterable[str], Mapping[str, Any]) -> str
~~~
~~~ python
credential = CertificateCredential(tenant_id=from_custom_config('tenant_id'),
                                   client_id=from_custom_config('client_id'),
                                   certificate_path=from_custom_config('cert_path'))
                                    
key_client = KeyClient("https://myvault.vault.azure.net/", credential)
~~~
__javascript__
~~~ typescript
export class ClientCertificateCredential implements TokenCredential {
  constructor(
    private tenantId: string,
    private clientId: string,
    private cetificatePath: string,
    options?: IdentityClientOptions) {
  }
}
~~~
~~~ typescript
const credential = new ClientSecretCredential(
    fromCustomConfig('tenantId'),
    fromCustomConfig('clientId'),
    fromCustomConfig('certPath'));
                                    
const keyClient = new KeyClient("https://myvault.vault.azure.net/", credential);
~~~
__java__
~~~ java
public class ClientCertificateCredential extends TokenCredential {
    public ClientCertificateCredential(String tenantId, String clientId, String certificatePath);

    public ClientCertificateCredential(String tenantId, String clientId, String certificatePath, IdentityClientOptions options);
}
~~~
~~~ java
ClientCertificateCredential credential = new ClientCertificateCredential(
    fromCustomConfig('tenantId'),
    fromCustomConfig('clientId'),
    fromCustomConfig('certPath'));
                                    
KeyClient keyClient = new KeyClient("https://myvault.vault.azure.net/", credential);
~~~
__C#__
~~~ c#
public class ClientCertificateCredential : TokenCredential
{
    public ClientCertificateCredential(string tenantId, string clientId, string path, IdentityClientOptions options = default);

    public ClientCertificateCredential(string tenantId, string clientId, byte[] thumbprint, IdentityClientOptions options = default);

    public ClientCertificateCredential(string tenantId, string clientId, X509Certificate2 certificate, IdentityClientOptions options = default);
}
~~~
~~~ c#
ClientCertificateCredential credential = new ClientCertificateCredential
(
    FromCustomConfig('tenantId'), 
    FromCustomConfig('clientId'),
    Convert.FromBase64String(FromCustomConfig('thumbprint'))
);

KeyClient keyClient = new KeyClient("https://myvault.vault.azure.net/", credential);
~~~

### __EnvironmentCredential__
The EnvironmentCredential type implements TokenCredential by pulling credential data from predefined environment variables.
- AZURE_TENANT_ID
- AZURE_CLIENT_ID
- AZURE_CLIENT_SECRET
- AZURE_CLIENT_CERTIFICATE_PATH
- AZURE_CLIENT_CERTIFICATE_THUMBPRINT

__python__
~~~ python
class EnvironmentCredential:
    @staticmethod
    def create_config(**kwargs):
        # type: (Mapping[str, str]) -> Configuration
        
    def __init__(config=None, **kwargs):
        # type: (Optional[Configuration], Mapping[str, Any]) -> None
    
    def get_token(self, scopes, **kwargs):
        # type: (Iterable[str], Mapping[str, Any]) -> str
~~~
~~~ python
key_client = KeyClient("https://myvault.vault.azure.net/", EnvironmentCredential())
~~~
__javascript__
~~~ typescript
export class EnvironmentCredential implements TokenCredential {
  constructor(options?: IdentityClientOptions) {
  }
}
~~~
~~~ typescript
const client = new KeysClient("https://myvault.vault.azure.net/", new EnvironmentCredential());
~~~
__java__
~~~ java
public class EnvironmentCredential extends TokenCredential {
    public EnvironmentCredential();

    public EnvironmentCredential(IdentityClientOptions options);
}
~~~
~~~ java
KeyClient keyClient = KeyClient.builder()
                            .endpoint("https://myvault.vault.azure.net/")
                            .credentials(new EnvironmentCredential())
                            .build();
~~~
__C#__
~~~ c#
public class EnvironmentCredential : TokenCredential
{
    public EnvironmentCredential();

    public EnvironmentCredential(IdentityClientOptions options);
}
~~~
~~~ c#
KeyClient keyClient = new KeyClient("https://myvault.vault.azure.net/", new EnvironmentCredential());
~~~

### __AggregateCredential__
__python__
~~~ python
class AggregateCredential:
    def __init__(self, credentials):
        # type: (Iterable[TokenCredential]) -> None
    
    def get_token(self, scopes, **kwargs):
        # type: (Iterable[str], Mapping[str, Any]) -> str
~~~
~~~ python
credential = AggregateCredential([EnvironmentCredential(), ManagedIdentityCredential(), CustomConfigCredential("config.json")])

key_client = KeyClient("https://myvault.vault.azure.net/", credential)
~~~
__javascript__
~~~ typescript
export class AggregateCredential implements TokenCredential {
  constructor(...sources: TokenCredential[]) {
    this._sources = sources
  }
}
~~~
~~~ typescript
const credential = new AggregateCredential(
    new EnvironmentCredential(), 
    new ManagedIdentityCredential(), 
    new CustomConfigCredential("config.json"));

const keyClient = KeyClient("https://myvault.vault.azure.net/", credential);
~~~
__java__
~~~ java
public class AggregateCredential extends TokenCredential {
    public AggregateCredential(TokenCredential... credentials);
}
~~~
~~~ java
AggregateCredential credential = new AggregateCredential(
    new EnvironmentCredential(), 
    new ManagedIdentityCredential(), 
    new CustomConfigCredential("config.json"));

KeyClient keyClient = KeyClient("https://myvault.vault.azure.net/", credential);
~~~
__C#__
~~~ c#
public class AggregateCredential : TokenCredential
{
    public AggregateCredential(params TokenCredential[] sources);
}
~~~
~~~ c#
var credential = new AggregateCredential
(
    new EnvironmentCredential(), 
    new ManagedIdentityCredential(), 
    new CustomConfigCredential("config.json")
);

var keyClient = new KeyClient("https://myvault.vault.azure.net/", credential);
~~~

### __IdentityClientOptions__

__python__
~~~ python
class EnvironmentCredential:
    def __init__(config=None, **kwargs):
        # type: (Optional[Configuration], Mapping[str, Any]) -> None
        # supports additional kwargs: authority refresh_buffer_ms
~~~
~~~ python
credential = EnvironmentCredential(authority='https://login.chinacloudapi.cn/', retry_total=5)
~~~
__javascript__
~~~ typescript
export interface IdentityClientOptions extends ServiceClientOptions {
  authority?: string
  refreshBufferMs?: number
}
~~~
~~~ typescript
const options = { authority: "https://login.chinacloudapi.cn/", noRetryPolicy: true };

const client = new KeysClient("https://myvault.vault.azure.net/", new EnvironmentCredential(options));
~~~
__java__
~~~ java
public class IdentityClientOptions {
    public IdentityClientOptions();

    public IdentityClientOptions authority(String endPoint);
    public IdentityClientOptions refreshBufferMs(int refreshBufferMs);
    public IdentityClientOptions httpLogDetailLevel(HttpLogDetailLevel logLevel);
    public IdentityClientOptions httpClient(HttpClient client);
    public IdentityClientOptions pipeline(HttpPipeline pipeline);
    public IdentityClientOptions addPolicy(HttpPipelinePolicy policy);
}
~~~
~~~ java
IdentityClientOptions options = new IdentityClientOptions()
                                    .authority("https://login.chinacloudapi.cn/")
                                    .httpLogDetailLevel(HttpLogDetailLevel.BODY_AND_HEADERS));

KeyClient keyClient = KeyClient.builder()
                            .endpoint("https://myvault.vault.azure.net/")
                            .credentials(new EnvironmentCredential(options))
                            .build();
~~~
__C#__
~~~ c#
public class IdentityClientOptions : HttpClientOptions
{
    public IdentityClientOptions();

    public RetryPolicy RetryPolicy { get; set; }
    public Uri Authority { get; set; }
    public TimeSpan RefreshBuffer { get; set; }
}
~~~
~~~ c#
var options = new IdentityClientOptions() 
{
    Authority = new Uri("https://login.chinacloudapi.cn/"),
};

options.TelemetryPolicy.ApplicationId = "MyApp";

var keyClient = new KeyClient("https://myvault.vault.azure.net/", new EnvironmentCredential(options));
~~~