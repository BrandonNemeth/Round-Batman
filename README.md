# Round-Batman

## Requirements

- .NET 10 SDK
- Python 3, if you want to run the web client

## Run the API

From the repository root, run:

```bash
dotnet restore
dotnet run --project src/RoundBatman.Api/RoundBatman.Api.csproj --launch-profile http
```

The API will be available at `http://localhost:5227`.

To check that it is running, open `http://localhost:5227/health`.

## Run the web client

With the API running, open another terminal and run:

```bash
cd client
python3 -m http.server 5500
```

Open `http://localhost:5500` in your browser. The client is configured to use
the API at `http://localhost:5227`.

## Run the tests

From the repository root, run:

```bash
dotnet test RoundBatman.slnx
```

## Run with Podman

The recommended deployment uses exactly two containers:

- `postgres`: PostgreSQL with a persistent named volume.
- `api`: the Round-Batman API, connected to PostgreSQL over a private container network.

The database is not exposed on the host. Only the API port is published.

Copy the example environment file and set a strong local password:

```bash
cp .env.example .env
```

Edit `.env`, then start both containers:

```bash
podman compose up --build
```

The API will be available at `http://localhost:8080`. The API container waits
for PostgreSQL, applies the EF Core migrations, and then starts the application.

Stop the containers with:

```bash
podman compose down
```

The database volume is preserved by `podman compose down`. To remove the local
database and all its data:

```bash
podman compose down --volumes
```

The API image includes the EF Core migration bundle. For Kubernetes, run
`/app/efbundle` as a separate migration Job before rolling out the API, and set
`RUN_MIGRATIONS=false` on API replicas.

### Podman without Compose

If a Compose provider is not installed, run the same two containers manually:

```bash
podman network create round-batman
podman volume create round-batman-postgres-data

podman run -d --name round-batman-postgres \
  --network round-batman \
  -e POSTGRES_DB=roundbatman \
  -e POSTGRES_USER=roundbatman \
  -e POSTGRES_PASSWORD='change-this-password' \
  -v round-batman-postgres-data:/var/lib/postgresql/data \
  docker.io/library/postgres:16

podman build -f Containerfile -t round-batman:local .

podman run -d --name round-batman-api \
  --network round-batman \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e 'ConnectionStrings__RoundBatman=Host=round-batman-postgres;Port=5432;Database=roundbatman;Username=roundbatman;Password=change-this-password' \
  localhost/round-batman:local
```

The database password must be the same in both containers. If the password
contains special characters, pass the complete connection string through an
environment file or a secret manager.
