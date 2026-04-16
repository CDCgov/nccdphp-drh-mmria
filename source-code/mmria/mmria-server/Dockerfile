# ----- Stage 1: Build -----
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-90:9.0.115-1776285362@sha256:10c0f1574c6175daf40cf1d9bb94d5d843919732d208db44d1a3e703aef19063 AS build
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
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-90-runtime:9.0.14-1776256365@sha256:d94af1155bf4be51248a45d42977efa6fa058429e3a1a6df35a5e5bfaa018b11 AS runtime

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
