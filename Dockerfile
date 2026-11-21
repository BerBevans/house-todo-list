# The production image. Human-owned: agents can't change this file.
# Only HouseTodoList.Web and the projects it references go in. Test projects, including the stub
# token issuer, are left out of the build context by .dockerignore and never reach this image.
# Base images are pinned by digest; Dependabot moves them.

FROM mcr.microsoft.com/dotnet/sdk:10.0.404-noble@sha256:fe0fd4e93bd2fbb47c883b7a6369e5c8d3db8bf01cdc5981f6ee35fbdf3ff4fb AS build
WORKDIR /repo
# CI=true makes the restore fail if a lock file is out of date, as it does in GitHub Actions.
ENV CI=true DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

# Build settings and package versions first, so the restore layer is cached until they change.
COPY global.json nuget.config Directory.Build.props Directory.Packages.props .editorconfig ./
COPY .github/SonarLint.xml .github/
COPY src/HouseTodoList.Core/*.csproj src/HouseTodoList.Core/packages.lock.json src/HouseTodoList.Core/
COPY src/HouseTodoList.Adapters/*.csproj src/HouseTodoList.Adapters/packages.lock.json src/HouseTodoList.Adapters/
COPY src/HouseTodoList.Web/*.csproj src/HouseTodoList.Web/packages.lock.json src/HouseTodoList.Web/
RUN dotnet restore src/HouseTodoList.Web/HouseTodoList.Web.csproj

COPY src/ src/
RUN dotnet publish src/HouseTodoList.Web/HouseTodoList.Web.csproj --no-restore -c Release -o /app
# The data folder, made here because the runtime image has no shell. A new volume mounted on it
# takes its ownership, so the app's user can write there.
RUN mkdir /data

# Chiseled: no shell or package manager, and it runs as the non-root `app` user. The extra variant
# carries ICU and time zone data, for British dates and times.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra@sha256:cae5b962bfb786606def03d250c6a645cc98839095c89e806e4bd8978a68115b
WORKDIR /app
COPY --from=build /app .
COPY --from=build --chown=1654:1654 /data /data
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "HouseTodoList.Web.dll"]
