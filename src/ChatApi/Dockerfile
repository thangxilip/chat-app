# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ChatApi.sln ./
COPY ChatApi/ChatApi.csproj ChatApi/
RUN dotnet restore ChatApi.sln

COPY ChatApi/ ChatApi/
RUN dotnet publish ChatApi/ChatApi.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ChatApi.dll"]
