FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-90:9.0.112-1764681922@sha256:ec784f172735873a5893504e07c57dcbd56b7b049a395c5629c6058dbfac21a3 AS build
WORKDIR /src
ENV GIT_SSL_NO_VERIFY=true

# Copy csproj files for better caching
COPY source-code/mmria/mmria-server/mmria-server.csproj ./source-code/mmria/mmria-server/
COPY nccdphp-drh-mmria-common/mmria.common/mmria.common.csproj ./nccdphp-drh-mmria-common/mmria.common/
COPY nccdphp-drh-mmria-common/mmria.getset/mmria.getset.csproj ./nccdphp-drh-mmria-common/mmria.getset/

USER root
WORKDIR /src/source-code/mmria/mmria-server
RUN dotnet restore mmria-server.csproj

# Copy source code
WORKDIR /src
COPY source-code/mmria/ ./source-code/mmria/
COPY nccdphp-drh-mmria-common/ ./nccdphp-drh-mmria-common/

# Build and publish in one step
WORKDIR /src/source-code/mmria/mmria-server
RUN dotnet publish mmria-server.csproj -c Release -f net9.0 -o /app/publish --no-restore

USER 1001

# Runtime stage
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-90-runtime:9.0-1765385235@sha256:43920d10408205a379519e16530d5181db65a79df3c2725b3cbad26798d09037 AS runtime
USER root
WORKDIR /app

COPY --from=build /app/publish .
COPY --from=build /src/source-code/mmria/mmria-server/database-scripts ./database-scripts
RUN rm -f ./wwwroot/test.html ./wwwroot/test2.html || true

RUN mkdir -p /opt/app-root/src/source-code/mmria/mmria-server/database-scripts && \
    cp -r ./database-scripts/* /opt/app-root/src/source-code/mmria/mmria-server/database-scripts/

EXPOSE 80 443
USER 1001
ENTRYPOINT ["dotnet", "mmria-server.dll"]