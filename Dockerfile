FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS publish
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1
WORKDIR /app
COPY ["Dark.csproj", "./"]
RUN apk add clang build-base zlib-dev
RUN dotnet restore --runtime alpine-x64
COPY . .
RUN dotnet publish \
    --configuration Release \
    --output /app/publish \
    # --no-restore \
    --runtime alpine-x64 \
    --self-contained true

FROM mcr.microsoft.com/dotnet/runtime-deps:7.0.1-alpine3.16 AS final
WORKDIR /app
RUN adduser --disabled-password --home /app --gecos "" dotnetuser
USER dotnetuser
EXPOSE 5000
COPY --from=publish --chown=dotnetuser:dotnetuser /app/publish/appsettings.json .
COPY --from=publish --chown=dotnetuser:dotnetuser /app/publish/Dark .
ENV ASPNETCORE_URLS=http://+:5000 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
ENTRYPOINT ["./Dark"]
