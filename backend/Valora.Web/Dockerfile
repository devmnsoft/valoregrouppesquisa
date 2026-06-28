FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/Valora.Web/Valora.Web.csproj backend/Valora.Web/
RUN dotnet restore backend/Valora.Web/Valora.Web.csproj
COPY backend/Valora.Web/ backend/Valora.Web/
RUN dotnet publish backend/Valora.Web/Valora.Web.csproj -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet","Valora.Web.dll"]
