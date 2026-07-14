# ----- Stage 1: Build -----
# https://catalog.redhat.com/en/software/containers/ubi8/dotnet-90/672bc9015e6e9c0c6f7d2ff7
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-90:9.0-1784003632@sha256:fbff3be85e7706fbb12afdf02d8f21faf5722f63bdda43150934d0d512374686 AS build
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
# https://catalog.redhat.com/en/software/containers/ubi8/dotnet-90-runtime/672bc8de5e6e9c0c6f7d2f65
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-90-runtime:9.0-1783975593@sha256:1b7266ecedfbcea13a3e2ae0857405402f31f77ec69fdcff52a6ac2b9eee1fce AS runtime

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
