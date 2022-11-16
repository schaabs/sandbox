FROM alpine/git as repo
RUN git clone https://github.com/schaabs/azure-sdk-for-net --single-branch --branch identity-arcsfmi --depth 1 /azure-sdk-for-net

FROM mcr.microsoft.com/dotnet/core/sdk:3.1

COPY --from=repo /azure-sdk-for-net/sdk/identity/ /sdkroot/sdk/identity/
COPY --from=repo /azure-sdk-for-net/sdk/core/ /sdkroot/sdk/core/
COPY --from=repo /azure-sdk-for-net/eng/ /sdkroot/eng/
COPY --from=repo /azure-sdk-for-net/tools/ /sdkroot/tools/
COPY --from=repo /azure-sdk-for-net/build.proj /sdkroot/
COPY --from=repo /azure-sdk-for-net/Directory.Build.props /sdkroot/
COPY --from=repo /azure-sdk-for-net/Directory.Build.targets /sdkroot/
COPY --from=repo /azure-sdk-for-net/global.json /sdkroot/
COPY --from=repo /azure-sdk-for-net/NuGet.Config /sdkroot/

ENV IDENTITYTEST_SFTEST_ENABLE true
ENV AZURE_TEST_MODE Record
ENV IDENTITYTEST_IMDSTEST_SYSTEMASSIGNEDVAULT https://aiimdssavault.vault.azure.net/

WORKDIR /sdkroot/sdk/identity/Azure.Identity

CMD dotnet test Azure.Identity.Tests.dll --filter "FullyQualifiedName~Azure.Identity.Tests.ManagedIdentityCredentialSFLiveTests"

CMD cat /home/sschaab/azure-sdk-for-net/sdk/identity/Azure.Identity/tests/SessionRecords/ManagedIdentityCredentialSFLiveTests/*


dotnet test Azure.Identity.Tests.dll --filter "FullyQualifiedName~Azure.Identity.Tests.ManagedIdentityCredentialSFLiveTests"