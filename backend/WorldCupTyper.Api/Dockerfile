FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/WorldCupTyper.sln backend/
COPY backend/WorldCupTyper.Api/WorldCupTyper.Api.csproj backend/WorldCupTyper.Api/
COPY backend/WorldCupTyper.Application/WorldCupTyper.Application.csproj backend/WorldCupTyper.Application/
COPY backend/WorldCupTyper.Domain/WorldCupTyper.Domain.csproj backend/WorldCupTyper.Domain/
COPY backend/WorldCupTyper.Infrastructure/WorldCupTyper.Infrastructure.csproj backend/WorldCupTyper.Infrastructure/

RUN dotnet restore backend/WorldCupTyper.Api/WorldCupTyper.Api.csproj

COPY backend/. backend/

RUN dotnet publish backend/WorldCupTyper.Api/WorldCupTyper.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WorldCupTyper.Api.dll"]
