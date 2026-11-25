# PostgreSQL on local kind

Deploy [postgres.yml](postgres.yml) to run Round-Batman's PostgreSQL database
inside Kubernetes. This setup is for local development, not production.

Unlike the Tournaments project, Round-Batman does not use a SQL initialization
script. The database and login role are created by the PostgreSQL image from
`POSTGRES_DB`/`POSTGRES_USER`, and the schema is created by the EF Core
migrations that the API container runs at startup.

The password lives in [postgres-secret.yml](postgres-secret.yml) and is the
local-only value `admin`, matching `.env.example`. Do not use it in a real
cluster.

## 1. Select your cluster

Requires `kind`, `kubectl`, and a running container runtime.

```bash
kind get clusters
# Replace kind with your cluster name if different.
kubectl config use-context kind-kind
kubectl cluster-info
kubectl get storageclass
```

Standard kind clusters provide the default StorageClass required by the
manifest. Check your context before deploying so you do not modify a different
cluster.

## 2. Deploy PostgreSQL

```bash
kubectl apply -f kubernetes/namespace.yml
kubectl apply -f database/postgres-secret.yml
kubectl apply -f database/postgres.yml

kubectl -n round-batman rollout status statefulset/round-batman-db --timeout=180s
kubectl -n round-batman logs round-batman-db-0
```

The StatefulSet keeps its data on a 5Gi PersistentVolumeClaim. Data survives
Pod restarts, but deleting the kind cluster removes its local storage. This is
not a backup.

## 3. Verify the database

The tables are created the first time the API container starts. After deploying
the API (see the main README), check the connection and the tables with:

```bash
curl --fail http://localhost:8080/health/db
# {"status":"ok","database":"connected"}

kubectl -n round-batman exec round-batman-db-0 -- \
  psql -U roundbatman -d roundbatman -c '\dt'
```

Expect `Teams`, `Tournaments`, `Groups`, `Matches`, and `GroupTeams`, plus the
EF Core `__EFMigrationsHistory` table. For an interactive session:

```bash
kubectl -n round-batman exec -it round-batman-db-0 -- \
  psql -U roundbatman -d roundbatman
```

## 4. Connect an application running on your machine

Leave this running in another terminal:

```bash
kubectl -n round-batman port-forward service/round-batman-db 5432:5432
```

Then override the connection string when starting the API:

```bash
ConnectionStrings__RoundBatman='Host=localhost;Port=5432;Database=roundbatman;Username=roundbatman;Password=admin' \
  dotnet run --project src/RoundBatman.Api/RoundBatman.Api.csproj --launch-profile http
```

If local port 5432 is occupied, forward `5433:5432` and use `Port=5433` above.

## Troubleshooting and resetting

```bash
kubectl -n round-batman get pods,pvc
kubectl -n round-batman describe pod round-batman-db-0
kubectl -n round-batman logs round-batman-db-0
kubectl -n round-batman logs round-batman-db-0 --previous
```

- A `Pending` PVC usually means no working default StorageClass. Inspect it with
  `kubectl -n round-batman describe pvc data-round-batman-db-0`.
- Changing the Secret or restarting the Pod does **not** change the password of
  an already-initialized data volume. Reset the volume or alter the role
  password explicitly.

To start from scratch, these commands **permanently delete all Round-Batman
database data**. Only run them if that is intended:

```bash
kubectl -n round-batman scale statefulset round-batman-db --replicas=0
kubectl -n round-batman wait --for=delete pod/round-batman-db-0 --timeout=120s
kubectl -n round-batman delete pvc data-round-batman-db-0
kubectl -n round-batman scale statefulset round-batman-db --replicas=1
kubectl -n round-batman rollout status statefulset/round-batman-db --timeout=180s
```

The API re-applies the EF Core migrations on its next startup.
