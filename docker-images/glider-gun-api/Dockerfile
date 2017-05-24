# Glider Gun API
#
# Build this with the context of the repository root folder (../.. relative to this file)

# If building from source
# FROM microsoft/dotnet:1.1.2-sdk

# If running output of "dotnet publish"
FROM microsoft/dotnet:1.1.2-runtime

MAINTAINER Adam Friedman <adam.friedman@itaas.dimensiondata.com>

RUN mkdir -p /var/lib/glider-gun/state
VOLUME /var/lib/glider-gun/state

EXPOSE 5050

#RUN mkdir -p /dotnetapp
WORKDIR /dotnetapp

#COPY NuGet.config /dotnetapp
    
# If building from source
#COPY src/GliderGun.Api /dotnetapp
#RUN ["dotnet", "restore"]
#RUN ["dotnet", "build"]

# If running output of "dotnet publish"
#COPY src/GliderGun.Api/bin/Release/netcoreapp1.1/publish  /dotnetapp

ENV GG_LocalStateDirectory=/var/lib/glider-gun/state
# If building from source
# ENTRYPOINT ["dotnet", "run"]

# If running output of "dotnet publish"
ENTRYPOINT ["dotnet", "GliderGun.Api.dll"]
