FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props LemonWriter.sln ./
COPY src/LemonWriter.API/LemonWriter.API.csproj src/LemonWriter.API/
COPY src/LemonWriter.Application/LemonWriter.Application.csproj src/LemonWriter.Application/
COPY src/LemonWriter.Domain/LemonWriter.Domain.csproj src/LemonWriter.Domain/
COPY src/LemonWriter.Infrastructure/LemonWriter.Infrastructure.csproj src/LemonWriter.Infrastructure/
RUN dotnet restore src/LemonWriter.API/LemonWriter.API.csproj

COPY src/ src/
COPY proto/ proto/
RUN dotnet publish src/LemonWriter.API/LemonWriter.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "LemonWriter.API.dll"]
