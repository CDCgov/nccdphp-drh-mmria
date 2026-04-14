# ----- Stage 1: Build -----
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-90:9.0.115-1775474558@sha256:e0ed055bfffb587dad25cfcaf2bdcbe360ea5708a1a22729539f433fb4d22406 AS build
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

RUN dotnet build mmria-server.csproj -c Release -f net9.0 -o /app/build
USER 1001

# ----- Stage 2: Publish -----
FROM build AS publish
USER root
WORKDIR /src/source-code/mmria/mmria-server
RUN dotnet publish mmria-server.csproj -c Release -f net9.0 -o /app/publish
USER 1001

# ----- Stage 3: Runtime -----
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-90-runtime:9.0.14-1775437908@sha256:998d3c84a0b2d9d69f93a3362d6459e440ebce3c206540640a87b2eee22bcd7e AS runtime

# Switch to root temporarily to create directories
USER root

WORKDIR /app
COPY --from=publish /app/publish .
RUN rm -f ./wwwroot/test.html ./wwwroot/test2.html || true
COPY --from=build /src/source-code/mmria/mmria-server/database-scripts ./database-scripts

# Create the expected directory structure for legacy code compatibility
RUN mkdir -p /opt/app-root/src/source-code/mmria/mmria-server/database-scripts && \
    cp -r ./database-scripts/* /opt/app-root/src/source-code/mmria/mmria-server/database-scripts/

EXPOSE 80
EXPOSE 443

# Switch back to non-root user
USER 1001

ENTRYPOINT ["dotnet", "mmria-server.dll"]
