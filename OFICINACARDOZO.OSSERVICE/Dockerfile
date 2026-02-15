# Trigger pipeline: alteração forçada em 15/02/2026
# Build
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["OFICINACARDOZO.OSSERVICE.csproj", "."]
RUN dotnet restore "./OFICINACARDOZO.OSSERVICE.csproj"
COPY . .
RUN dotnet publish "./OFICINACARDOZO.OSSERVICE.csproj" -c Release -o /app/publish

# Final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "OFICINACARDOZO.OSSERVICE.dll"]
