# ----- Stage 1: Build -----
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100:9.8-1785892712@sha256:05a11063b0915667415a1b57b8aff2e69b45df16341f29258ae56a0dafdcd74d AS build
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
# https://console-openshift-console.apps.ecpaas-dev.cdc.gov/k8s/ns/trusted-images/imagestreamtags/dotnet-100-aspnet%3A9.7-1776776188
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100-aspnet:9.8-1785861844@sha256:d3606705050f499b0911bbd54dce2103e5df428f54f0c1a77d450eb1f6437e1a AS runtime
WORKDIR /app

# Update libacl to address CVE-2026-54369 (symlink traversal privilege escalation;
# fixed in libacl-2.4.0-1.el9_8). Fall back to microdnf for UBI-minimal variants.
USER root
RUN (dnf update -y libacl && dnf clean all) || (microdnf update -y libacl && microdnf clean all)

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
