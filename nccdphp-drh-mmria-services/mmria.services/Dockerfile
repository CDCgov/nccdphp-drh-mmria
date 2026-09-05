# ----- Stage 1: Build -----
# https://catalog.redhat.com/en/software/containers/ubi9/dotnet-100/68c12c6b1359271d5a1b0461
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100:9.8-1787019421@sha256:e9f29ea12f22b15a784d5e43caba81de6f3b6c5fa4674c74fb95a362b216daa5 AS build
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
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100-aspnet:9.8-1786442384@sha256:4c6dc7a25515d540eb5672287078d0c042075c8963ba9306f770032277ee8206 AS runtime
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
