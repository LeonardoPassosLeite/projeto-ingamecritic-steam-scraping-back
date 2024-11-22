# Imagem base para o runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Imagem base para o build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SteamChartsAPI.csproj", "./"]
RUN dotnet restore "./SteamChartsAPI.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "SteamChartsAPI.csproj" -c Release -o /app/build

# Publicação
FROM build AS publish
RUN dotnet publish "SteamChartsAPI.csproj" -c Release -o /app/publish

# Configuração final para o runtime
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SteamChartsAPI.dll"]
