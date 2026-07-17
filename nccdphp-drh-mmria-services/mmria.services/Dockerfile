# ----- Stage 1: Build -----
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100:9.8-1782912189@sha256:ca21a4d4f70f87c4582a6f5b4463e61c996fb34684985c6622a743560ae10e4a AS build
WORKDIR /src

# Copy the mmria.services project file first to leverage layer caching for restores.
# (COPY with a wildcard so the same Dockerfile works if the csproj name changes slightly)
COPY nccdphp-drh-mmria-services/mmria.services/*.csproj ./nccdphp-drh-mmria-services/mmria.services/

# Copy common dependencies (mmria.common and mmria.getset) from canonical location
COPY nccdphp-drh-mmria-common/ ./nccdphp-drh-mmria-common/

# Copy the rest of mmria.services source
COPY nccdphp-drh-mmria-services/mmria.services/ ./nccdphp-drh-mmria-services/mmria.services/

USER root
WORKDIR /src/nccdphp-drh-mmria-services/mmria.services

# Restore (will use the cached layer when csproj hasn't changed)
RUN dotnet restore

# Build specifying the targeted framework (project targets multiple frameworks)
RUN dotnet build -c Release -f net10.0 -o /app/build

USER 1001

# ----- Stage 2: Publish -----
FROM build AS publish
USER root
WORKDIR /src/nccdphp-drh-mmria-services/mmria.services

# Publish the specific target framework to reduce final image size
RUN dotnet publish -c Release -f net10.0 -o /app/publish

USER 1001

# ----- Stage 3: Runtime -----
# https://console-openshift-console.apps.ecpaas-dev.cdc.gov/k8s/ns/trusted-images/imagestreamtags/dotnet-100-aspnet%3A9.8-1784176949
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100-aspnet:9.8-1784176949@sha256:05b9c57ecbce6dcc11b8e2a82184e3d8063508618bd616fc713e0f953816bfd1 AS runtime
WORKDIR /app

# Copy published output from build image
COPY --from=publish /app/publish .

# Let the app bind to 8080 (OpenShift routing) unless overridden by environment
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Run as non-root user (OpenShift friendly)
USER 1001

# NOTE: the published assembly name is assumed to be mmria.services.dll.
# If your project produces a different assembly name, update the ENTRYPOINT accordingly.
ENTRYPOINT ["dotnet", "mmria.services.dll"]
