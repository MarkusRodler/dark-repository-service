FROM mcr.microsoft.com/dotnet/nightly/sdk:10.0.102-noble-aot AS publish
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1
WORKDIR /app
COPY . .
RUN --mount=type=cache,target=/root/.nuget \
    --mount=type=cache,target=/app/artifacts \
    dotnet publish Service \
    --output /app/publish \
    --runtime linux-x64 \
    --self-contained true \
    /p:DebugType=None \
    /p:DebugSymbols=false
RUN rm -rf publish/*.dbg \
    publish/*.Development.json

FROM mcr.microsoft.com/dotnet/nightly/runtime-deps:9.0.11-noble-chiseled-aot
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["./Service"]
