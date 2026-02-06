FROM mcr.microsoft.com/dotnet/sdk:8.0 AS installer-env
USER root

COPY EprPrnIntegration.Common/*.csproj ./EprPrnIntegration.Common/
COPY EprPrnIntegration.Api/*.csproj ./EprPrnIntegration.Api/

# Restore
RUN dotnet restore EprPrnIntegration.Api/*.csproj

COPY EprPrnIntegration.Common/. ./EprPrnIntegration.Common/.
COPY EprPrnIntegration.Api/. ./EprPrnIntegration.Api/.

# Publish 
RUN dotnet publish EprPrnIntegration.Api/*.csproj --no-restore --output /home/site/wwwroot

# Use the Azure Functions image as the final image
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0

# Create a non-root user and set permissions
RUN groupadd -r dotnet && \
    useradd -r -g dotnet dotnet && \
    chown -R dotnet /home

# Switch to the non-root user for the final image
USER dotnet

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]