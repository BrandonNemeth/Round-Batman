FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RoundBatman.slnx ./
COPY src/RoundBatman.Api/RoundBatman.Api.csproj src/RoundBatman.Api/
COPY src/RoundBatman.Delegates/RoundBatman.Delegates.csproj src/RoundBatman.Delegates/
COPY src/RoundBatman.Repositories/RoundBatman.Repositories.csproj src/RoundBatman.Repositories/
COPY src/RoundBatman.Domain/RoundBatman.Domain.csproj src/RoundBatman.Domain/

RUN dotnet restore src/RoundBatman.Api/RoundBatman.Api.csproj --runtime linux-x64

COPY src/ src/
RUN dotnet publish src/RoundBatman.Api/RoundBatman.Api.csproj -c Release -o /app --no-restore
RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.12
RUN /tools/dotnet-ef migrations bundle \
    --project src/RoundBatman.Repositories/RoundBatman.Repositories.csproj \
    --startup-project src/RoundBatman.Api/RoundBatman.Api.csproj \
    --self-contained \
    --runtime linux-x64 \
    --configuration Release \
    --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
COPY container/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["/entrypoint.sh"]
