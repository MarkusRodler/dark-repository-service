FROM mcr.microsoft.com/cbl-mariner/base/core:2.0 AS publish
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1
RUN tdnf -y install dotnet-sdk-6.0 ca-certificates-microsoft
WORKDIR /app
COPY ["Dark.csproj", "./"]
RUN dotnet restore "Dark.csproj" --runtime linux-x64
COPY . .
RUN dotnet publish "Dark.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    --runtime linux-x64 \
    --self-contained true

# Not working because of inaccessable folder
# See: https://stackoverflow.com/questions/55394567/mount-volumes-as-non-root-user-in-docker-container
# FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-cbl-mariner2.0-distroless AS final
# WORKDIR /app
# EXPOSE 5000
# COPY --from=publish --chown=app:app /app/publish .
# ENV ASPNETCORE_URLS=http://+:5000
# ENTRYPOINT ["./Dark"]

# Modified version from CBL-Mariner Repository
# See: https://github.com/dotnet/dotnet-docker/blob/main/src/runtime-deps/6.0/cbl-mariner2.0-distroless/amd64/Dockerfile
FROM mcr.microsoft.com/cbl-mariner/base/core:2.0 AS installer
RUN mkdir /staging \
    && tdnf install -y --releasever=2.0 --installroot /staging \
    prebuilt-ca-certificates \
    glibc \
    krb5 \
    libgcc \
    libstdc++ \
    openssl-libs \
    zlib \
    && tdnf clean all

RUN tdnf install -y shadow-utils \
    && tdnf clean all \
    && groupadd --system --gid=1000 app \
    && adduser --uid 1000 --gid app --shell /bin/false --no-create-home --system app \
    && cp /etc/passwd /staging/etc/passwd \
    && cp /etc/group /staging/etc/group

RUN rm -rf /staging/etc/dnf \
    && rm -rf /staging/run/* \
    && rm -rf /staging/var/cache/dnf \
    && find /staging/var/log -type f -size +0 -delete

FROM mcr.microsoft.com/cbl-mariner/distroless/minimal:2.0 as final
COPY --from=installer /staging/ /
USER app
WORKDIR /app
EXPOSE 5000
COPY --from=publish --chown=app:app /app/publish .
ENV ASPNETCORE_URLS=http://+:5000 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
ENTRYPOINT ["./Dark"]
