# ----- Stage 1: Build -----
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-100:9.8-1786438823@sha256:5e17d5b16b55828defad53f99e85cdec6ec90e6821c5f4f1a731c669a8509bce AS build
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

# Update libacl (CVE-2026-54369) and curl-minimal/libcurl-minimal (CVE-2026-11352,
# CVE-2026-11586, CVE-2026-8286, CVE-2026-8925, CVE-2026-9547) so that errata RPMs
# are picked up automatically on the next image rebuild once Red Hat publishes fixes.
# Fall back to microdnf for UBI-minimal variants.
USER root
RUN (dnf update -y libacl curl-minimal libcurl-minimal && dnf clean all) || \
    (microdnf update -y libacl curl-minimal libcurl-minimal && microdnf clean all)

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
