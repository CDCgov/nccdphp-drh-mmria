# ----- Stage 1: Build -----
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-90:9.0.112-1764681922@sha256:ec784f172735873a5893504e07c57dcbd56b7b049a395c5629c6058dbfac21a3 AS build
WORKDIR /src

# Define APP_VERSION as a build argument with default value
ARG APP_VERSION=25.10.14
ARG GIT_COMMIT=unknown
ENV APP_VERSION=${APP_VERSION}

# Copy only the csproj for dependency caching
COPY source-code/mmria/mmria-server/mmria-server.csproj ./source-code/mmria/mmria-server/

# Copy the rest of the source code
COPY source-code/mmria/ ./source-code/mmria/

USER root

RUN current_year=$(date +%y) && \
    current_month=$(date +%m) && \
    current_day=$(date +%d) && \
    current_build=$(echo "${GIT_COMMIT}" | cut -c1-7) && \
    echo "---> Replacing version with: $current_year.$current_month.$current_day v($current_build)" && \
    sed -e "s/<%=version%>/${current_year}.${current_month}.${current_day} v(${current_build})/g" \
        ./source-code/mmria/mmria-server/Views/Shared/_Footer.cshtml > /tmp/_Footer.cshtml.tmp && \
    mv /tmp/_Footer.cshtml.tmp ./source-code/mmria/mmria-server/Views/Shared/_Footer.cshtml && \
    echo "---> Version Setup Complete"

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
FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-90-runtime:9.0-1765385235@sha256:43920d10408205a379519e16530d5181db65a79df3c2725b3cbad26798d09037 AS runtime

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
