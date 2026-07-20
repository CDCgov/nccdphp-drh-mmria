# ----- Stage 1: Build -----
# https://catalog.redhat.com/en/software/containers/ubi8/dotnet-90/672bc9015e6e9c0c6f7d2ff7
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100:9.8-1782912189@sha256:ca21a4d4f70f87c4582a6f5b4463e61c996fb34684985c6622a743560ae10e4a AS build
WORKDIR /src

# Copy only the csproj for dependency caching
COPY source-code/mmria/mmria-server/mmria-server.csproj ./source-code/mmria/mmria-server/
COPY nccdphp-drh-mmria-common/mmria.common/mmria.common.csproj ./nccdphp-drh-mmria-common/mmria.common/

# Copy the rest of the source code
COPY source-code/mmria/ ./source-code/mmria/
COPY nccdphp-drh-mmria-common/ ./nccdphp-drh-mmria-common/

USER root
WORKDIR /src/source-code/mmria/mmria-server
RUN dotnet restore mmria-server.csproj

RUN dotnet build mmria-server.csproj -c Release -f net10.0 -o /app/build
USER 1001

# ----- Stage 2: Publish -----
FROM build AS publish
USER root
WORKDIR /src/source-code/mmria/mmria-server
RUN dotnet publish mmria-server.csproj -c Release -f net10.0 -o /app/publish
USER 1001

# ----- Stage 3: Runtime -----
# https://console-openshift-console.apps.ecpaas-dev.cdc.gov/k8s/ns/trusted-images/imagestreamtags/dotnet-100-aspnet%3A9.8-1784176949
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100-aspnet:9.8-1784176949@sha256:05b9c57ecbce6dcc11b8e2a82184e3d8063508618bd616fc713e0f953816bfd1 AS runtime

# Switch to root temporarily to create directories
USER root

WORKDIR /app
COPY --from=publish /app/publish .
RUN rm -f ./wwwroot/test*.html ./wwwroot/test.js ./wwwroot/pdf-version/test*.html ./wwwroot/print-version/test*.html || true
COPY --from=build /src/source-code/mmria/mmria-server/database-scripts ./database-scripts

# Create the expected directory structure for legacy code compatibility
RUN mkdir -p /opt/app-root/src/source-code/mmria/mmria-server/database-scripts && \
    cp -r ./database-scripts/* /opt/app-root/src/source-code/mmria/mmria-server/database-scripts/

EXPOSE 80
EXPOSE 443

# Switch back to non-root user
USER 1001

ENTRYPOINT ["dotnet", "mmria-server.dll"]
