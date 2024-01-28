FROM mcr.microsoft.com/dotnet/nightly/sdk:8.0-jammy-aot AS publish
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1
WORKDIR /app
COPY . .
RUN --mount=type=cache,target=/root/.nuget \
    --mount=type=cache,target=/app/artifacts \
    dotnet publish \
    --output /app/publish \
    --runtime linux-x64 \
    --self-contained true \
    /p:DebugType=None \
    /p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/nightly/runtime-deps:8.0-jammy-chiseled-aot
WORKDIR /app
COPY --from=publish /app/publish/appsettings.json .
COPY --from=publish /app/publish/Dark .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["./Dark"]
