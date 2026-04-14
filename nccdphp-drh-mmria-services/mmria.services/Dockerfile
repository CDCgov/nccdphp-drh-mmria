# ----- Stage 1: Build -----
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-90:9.0.115-1775474558@sha256:e0ed055bfffb587dad25cfcaf2bdcbe360ea5708a1a22729539f433fb4d22406 AS build
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
RUN dotnet build -c Release -f net9.0 -o /app/build

USER 1001

# ----- Stage 2: Publish -----
FROM build AS publish
USER root
WORKDIR /src/nccdphp-drh-mmria-services/mmria.services

# Publish the specific target framework to reduce final image size
RUN dotnet publish -c Release -f net9.0 -o /app/publish

USER 1001

# ----- Stage 3: Runtime -----
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-90-runtime:9.0.14-1775437908@sha256:998d3c84a0b2d9d69f93a3362d6459e440ebce3c206540640a87b2eee22bcd7e AS runtime
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
