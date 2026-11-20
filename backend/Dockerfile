FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/MiniErp.Api/MiniErp.Api.csproj src/MiniErp.Api/
RUN dotnet restore src/MiniErp.Api/MiniErp.Api.csproj
COPY src/ src/
RUN dotnet publish src/MiniErp.Api/MiniErp.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "MiniErp.Api.dll"]
