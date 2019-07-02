using System;

namespace app_service_msi_test
{
    class Program
    {
        static void Main(string[] args)
        {
            var credential = new ManagedIdentityCredential();

            var token = await credential.GetTokenAsync(new string[] { "https://management.azure.com//.default" });

            Console.WriteLine(token.Token);

            Console.WriteLine(token.ExpiresOn);
        }
    }
}
