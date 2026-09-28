# Build context: ./backend  (ver docker-compose.yml na raiz)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Restore em camada separada (cache): só os arquivos de projeto primeiro
COPY ["Directory.Build.props", "Directory.Packages.props", "global.json", "./"]
COPY ["src/FieldFlow.Domain/FieldFlow.Domain.csproj", "src/FieldFlow.Domain/"]
COPY ["src/FieldFlow.Application/FieldFlow.Application.csproj", "src/FieldFlow.Application/"]
COPY ["src/FieldFlow.Infrastructure/FieldFlow.Infrastructure.csproj", "src/FieldFlow.Infrastructure/"]
COPY ["src/FieldFlow.Api/FieldFlow.Api.csproj", "src/FieldFlow.Api/"]
RUN dotnet restore "src/FieldFlow.Api/FieldFlow.Api.csproj"
COPY src/ src/
RUN dotnet build "src/FieldFlow.Api/FieldFlow.Api.csproj" -c $BUILD_CONFIGURATION --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/FieldFlow.Api/FieldFlow.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# pasta de logs gravável pelo usuário não-root da imagem
USER root
RUN mkdir -p /app/logs && chown $APP_UID /app/logs
USER $APP_UID
ENTRYPOINT ["dotnet", "FieldFlow.Api.dll"]
