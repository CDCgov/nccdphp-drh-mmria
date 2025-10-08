# ----- Stage 1: Build -----
FROM image-registry.openshift-image-registry.svc:5000/dotnet-90@sha256:c34a55b413118073024ab517352d7af930cfac369baf305e3134457762f16042 AS build
WORKDIR /src
COPY ["mmria-server.csproj", ""]
USER root
RUN dotnet restore mmria-server.csproj
COPY . .
WORKDIR /src
RUN dotnet build mmria-server.csproj -c Release -o /app/build
USER 1001


FROM build AS publish

USER root
RUN dotnet publish mmria-server.csproj -c Release -o /app/publish
USER 1001

FROM default-route-openshift-image-registry.apps.ecpaas-dev.cdc.gov/trusted-images/dotnet-90-runtime:9.0.9-1758211314@sha256:699c83e64b5e931eb0158c803de38cfe176d1a107030ae3b07f678e15c37a88e AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
EXPOSE 443
USER root

USER 1001

ENTRYPOINT ["dotnet", "mmria-server.dll"]