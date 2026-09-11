# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia solo los archivos de proyecto primero (mejor cache de capas)
COPY RoundBatman.slnx ./
COPY src/RoundBatman.Api/RoundBatman.Api.csproj src/RoundBatman.Api/
COPY src/RoundBatman.Delegates/RoundBatman.Delegates.csproj src/RoundBatman.Delegates/
COPY src/RoundBatman.Repositories/RoundBatman.Repositories.csproj src/RoundBatman.Repositories/
COPY src/RoundBatman.Domain/RoundBatman.Domain.csproj src/RoundBatman.Domain/

RUN dotnet restore src/RoundBatman.Api/RoundBatman.Api.csproj

# Copia el resto del código y publica
COPY src/ src/
RUN dotnet publish src/RoundBatman.Api/RoundBatman.Api.csproj -c Release -o /app --no-restore

# Etapa final — solo runtime, imagen mucho más chica
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "RoundBatman.Api.dll"]
