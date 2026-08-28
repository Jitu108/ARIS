# Build context: repository root (docker build -f src/Services/IdentityService/aris.IdentityService.Api/Dockerfile .)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/BuildingBlocks/aris.BuildingBlocks/aris.BuildingBlocks.csproj src/BuildingBlocks/aris.BuildingBlocks/
COPY src/Services/IdentityService/aris.IdentityService.Domain/aris.IdentityService.Domain.csproj src/Services/IdentityService/aris.IdentityService.Domain/
COPY src/Services/IdentityService/aris.IdentityService.Application/aris.IdentityService.Application.csproj src/Services/IdentityService/aris.IdentityService.Application/
COPY src/Services/IdentityService/aris.IdentityService.Infrastructure/aris.IdentityService.Infrastructure.csproj src/Services/IdentityService/aris.IdentityService.Infrastructure/
COPY src/Services/IdentityService/aris.IdentityService.Api/aris.IdentityService.Api.csproj src/Services/IdentityService/aris.IdentityService.Api/

RUN dotnet restore src/Services/IdentityService/aris.IdentityService.Api/aris.IdentityService.Api.csproj

COPY src/BuildingBlocks/aris.BuildingBlocks/ src/BuildingBlocks/aris.BuildingBlocks/
COPY src/Services/IdentityService/ src/Services/IdentityService/

RUN dotnet publish src/Services/IdentityService/aris.IdentityService.Api/aris.IdentityService.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# curl is needed for the compose healthcheck against /health/ready (image ships with neither
# curl nor wget by default).
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "aris.IdentityService.Api.dll"]
