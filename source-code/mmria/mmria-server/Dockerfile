# ----- Stage 1: Build -----
# https://catalog.redhat.com/en/software/containers/ubi8/dotnet-90/672bc9015e6e9c0c6f7d2ff7
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100:9.8-1785401075@sha256:25cd4f9ebda2abe0f7c83a536437d246b39ca6837f97b669bd2f4b0f029fa7bd AS build
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
# https://console-openshift-console.apps.ecpaas-dev.cdc.gov/k8s/ns/trusted-images/imagestreamtags/dotnet-100-aspnet%3A9.7-1776776188
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100-aspnet:9.8-1785861844@sha256:d3606705050f499b0911bbd54dce2103e5df428f54f0c1a77d450eb1f6437e1a AS runtime

# Switch to root temporarily to create directories and patch OS packages
USER root

# Update libacl to address CVE-2026-54369 (symlink traversal privilege escalation;
# fixed in libacl-2.4.0-1.el9_8). Fall back to microdnf for UBI-minimal variants.
RUN (dnf update -y libacl && dnf clean all) || (microdnf update -y libacl && microdnf clean all)

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
