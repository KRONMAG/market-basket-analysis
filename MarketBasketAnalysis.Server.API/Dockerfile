FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER root
WORKDIR /app
RUN mkdir /app/data
RUN GRPC_HEALTH_PROBE_VERSION=v0.4.13 && \
    wget -qO/bin/grpc_health_probe https://github.com/grpc-ecosystem/grpc-health-probe/releases/download/${GRPC_HEALTH_PROBE_VERSION}/grpc_health_probe-linux-amd64 && \
    chmod +x /bin/grpc_health_probe
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MarketBasketAnalysis.Server.API/MarketBasketAnalysis.Server.API.csproj", "MarketBasketAnalysis.Server.API/"]
RUN dotnet restore "./MarketBasketAnalysis.Server.API/MarketBasketAnalysis.Server.API.csproj"
COPY . .
WORKDIR "/src/MarketBasketAnalysis.Server.API"
RUN dotnet build "./MarketBasketAnalysis.Server.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MarketBasketAnalysis.Server.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER app
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT dotnet MarketBasketAnalysis.Server.API.dll