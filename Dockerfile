FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copiar nuget.config y el feed de NuGet local
COPY nuget.config ./
COPY LocalNuGetFeed/ ./LocalNuGetFeed/

# Copiar csproj y restaurar dependencias
COPY src/OrderService/OrderService.csproj ./src/OrderService/
RUN dotnet restore src/OrderService/OrderService.csproj

# Copiar el código fuente y publicar
COPY src/OrderService/ ./src/OrderService/
RUN dotnet publish src/OrderService/OrderService.csproj -c Release -o out

# Imagen de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderService.dll"]
