FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS publish
WORKDIR /app
COPY ["Dark.csproj", "./"]
RUN dotnet restore "Dark.csproj" --runtime alpine-x64
COPY . .
RUN dotnet publish "Dark.csproj" -c Release -p:PublishReadyToRun=true -o /app/publish \
    --no-restore \
    --runtime alpine-x64 \
    --self-contained true \
    /p:PublishTrimmed=true \
    /p:PublishSingleFile=true

FROM mcr.microsoft.com/dotnet/runtime-deps:5.0-alpine AS final
WORKDIR /app
ENV TZ=Europe/Berlin
RUN adduser --disabled-password --home /app --gecos '' dotnetuser \
    && chown -R dotnetuser /app \
    && apk --no-cache add -U tzdata \
    && cp /usr/share/zoneinfo/${TZ} /etc/localtime \
    && echo "${TZ}" > /etc/timezone
USER dotnetuser
EXPOSE 5000
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["./Dark"]
