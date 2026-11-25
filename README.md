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

To check that it is running, open `http://localhost:5227/health`. To confirm the
API can also reach PostgreSQL, open `http://localhost:5227/health/db`.

## Run the web client

With the API running, open another terminal and run:

```bash
cd client
python3 -m http.server 5500
```

Open `http://localhost:5500` in your browser. The client is configured to use
the API at `http://localhost:5227`.

## Run the development CLI

From the repository root, start the interactive local launcher:

```bash
python3 dev.py
```

It can start/stop/status the API and database using either Podman Compose or the
local kind cluster, start/stop the static Web UI, preview and run a SQL query for
the latest completed matches, and run Locust interactively or headless. Locust
prompts for pure round robin vs. hybrid format, users, spawn rate, duration, and
teams per tournament. **Show recent matches** asks for a count and environment,
prints the exact read-only SQL first, then asks before executing it. The Web UI is
available at `http://localhost:5500`; set its **URL de la API**
field to `http://localhost:8080` when using Compose or kind.

The first start prompts for a PostgreSQL password and saves it in the ignored
`.env` file with owner-only permissions. Kind receives the password as a
Kubernetes Secret generated at runtime; it is not read from the local-only
`database/postgres-secret.yml`. Compose and kind stop actions preserve database
data. Stopping kind scales down the application workloads but leaves the cluster
and its persistent volume in place.

Kind actions use the `kind-kind` context and may create the `kind` cluster when
it does not exist. They require `kind`, `kubectl`, and Podman. Compose actions
require Podman Compose. Locust must be installed as described in
[`load_test/README.md`](load_test/README.md).

## Run the tests

From the repository root, run:

```bash
dotnet test RoundBatman.slnx
```

### Tournament progression

Both formats use a single round-robin group stage. A `ROUND_ROBIN` tournament
ends after that stage and saves the standings leader as champion. A `HYBRID`
tournament takes the top two teams into an automatic single-elimination bracket;
knockout rounds advance after all matches in the current round finish, including
byes when needed. The bracket supports up to 32 configured teams (five knockout
rounds at most). Existing tournaments are migrated to `HYBRID` to preserve their
previous group-stage-plus-knockout behavior. The champion is exposed as
`ChampionTeamId`; `Stage`/`Round` are included on each match. Completed matches
record `CompletedAt`; `GET /tournaments/{id}/matches/recent` returns the 20 most
recently completed matches without deleting older history.

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
podman-compose up --build -d
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

## Run on Kubernetes (kind)

The repository includes the same local Kubernetes layout used by the Tournaments
project, adapted for Round-Batman:

- `kind.yml`: maps host `8080` to the API NodePort `30080`, host `3000` to Grafana `30030`, and host `9090` to Prometheus `30090`.
- `database/postgres.yml`: PostgreSQL StatefulSet, Service, and persistent volume.
- `database/postgres-secret.yml`: local-only database password (`admin`).
- `kubernetes/namespace.yml`: the `round-batman` namespace.
- `kubernetes/app-config.yml`: shared API configuration.
- `kubernetes/api.yml`: API Deployment and NodePort Service.

The API image runs the EF Core migration bundle before starting, so no separate
migration Job or SQL initialization script is required. See
[database/README.md](database/README.md) for the database-only workflow and how
to reset local data.

### 1. Create the cluster

Requires `podman`, `kind`, and `kubectl`. If you already have a kind cluster with
the port mapping from `kind.yml`, skip this step and select its context.

```bash
kind create cluster --name kind --config kind.yml
kubectl config use-context kind-kind
```

Port mappings cannot be added to an existing kind cluster. Either create the
cluster with `kind.yml` or use port-forwarding in step 4.

### 2. Build and load the image

```bash
podman build -f Containerfile -t localhost/round-batman:dev .

# kind needs a Docker-compatible archive when the image is built with Podman.
podman save --format docker-archive -o /tmp/round-batman.tar localhost/round-batman:dev
kind load image-archive /tmp/round-batman.tar --name kind
rm /tmp/round-batman.tar
```

If you use Docker instead, build with the same tag and run
`kind load docker-image localhost/round-batman:dev --name kind`.

### 3. Deploy PostgreSQL and the API

```bash
kubectl apply -f kubernetes/namespace.yml
kubectl apply -f database/postgres-secret.yml
kubectl apply -f database/postgres.yml

kubectl -n round-batman rollout status statefulset/round-batman-db --timeout=180s

kubectl apply -f kubernetes/app-config.yml
kubectl apply -f kubernetes/api.yml
kubectl -n round-batman rollout status deployment/round-batman-api --timeout=180s
```

The API connects to `round-batman-db` over the cluster network; the database is
not exposed on the host.

### 4. Verify

With the cluster created from `kind.yml`, the API is available at
`http://localhost:8080` without port-forwarding:

```bash
curl --fail http://localhost:8080/health
curl --fail http://localhost:8080/health/db
curl --fail http://localhost:8080/teams
```

`/health` only checks that the API process is up. `/health/db` runs a connection
check against PostgreSQL and returns
`{"status":"ok","database":"connected"}` when it succeeds.

For an existing cluster without the port mapping, run this in another terminal:

```bash
kubectl -n round-batman port-forward service/round-batman-api 8080:8080
```

Inspect the database directly:

```bash
kubectl -n round-batman exec round-batman-db-0 -- \
  psql -U roundbatman -d roundbatman -c '\dt'
```

### 5. Rebuild or clean up

After changing application code, rebuild and reload the image (step 2), then
restart the Deployment:

```bash
kubectl -n round-batman rollout restart deployment/round-batman-api
kubectl -n round-batman rollout status deployment/round-batman-api --timeout=180s
```

Useful troubleshooting commands:

```bash
kubectl -n round-batman get pods,services,pvc
kubectl -n round-batman logs deployment/round-batman-api --tail=100
kubectl -n round-batman logs round-batman-db-0 --tail=100
kubectl -n round-batman get events --sort-by=.metadata.creationTimestamp
```

`ErrImageNeverPull` means the image was not loaded into the selected kind
cluster with the exact `localhost/round-batman:dev` tag.

### 6. Optional: monitoring and load testing

The API exposes Prometheus metrics at `/metrics`. A local
`kube-prometheus-stack` installation provides Prometheus and Grafana, and the
Locust scenario exercises every implemented API route end to end.

```bash
# Prometheus + Grafana (Grafana: admin/admin).
helm upgrade --install monitoring prometheus-community/kube-prometheus-stack \
  --version 92.18.0 --namespace monitoring --create-namespace --wait \
  -f kubernetes/monitoring/prometheus-values.yml
kubectl apply -f kubernetes/monitoring/api-servicemonitor.yml
kubectl apply -f kubernetes/monitoring/round-batman-dashboard.yml

# Locust (install once in a virtual environment).
python3 -m venv ~/.venvs/round-batman-load
~/.venvs/round-batman-load/bin/pip install -r load_test/requirements.txt
~/.venvs/round-batman-load/bin/locust -f load_test/locustfile.py \
  --host http://localhost:8080 --tournament-type HYBRID --headless \
  --users 5 --spawn-rate 1 --run-time 3m --teams-per-tournament 8
```

| Tool | URL |
| --- | --- |
| API | <http://localhost:8080> |
| Grafana | <http://localhost:3000> |
| Prometheus | <http://localhost:9090> |

See [kubernetes/monitoring/README.md](kubernetes/monitoring/README.md) for the
full monitoring guide and [load_test/README.md](load_test/README.md) for the
Locust options and PromQL examples.

To remove the application and its database data:

```bash
kubectl delete namespace round-batman
```

Deleting the kind cluster itself also removes all local database storage:

```bash
kind delete cluster --name kind
```

