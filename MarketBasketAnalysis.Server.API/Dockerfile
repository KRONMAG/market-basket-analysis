FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER app
WORKDIR /app
RUN mkdir /app/data
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
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT dotnet MarketBasketAnalysis.Server.API.dll