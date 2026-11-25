# Local Prometheus and Grafana for Round-Batman

This directory adds a local-only monitoring stack to the existing **kind**
cluster and scrapes the Round-Batman API's `/metrics` endpoint.

`kind.yml` maps the host ports used below:

| Tool | Host URL | Service NodePort |
| --- | --- | --- |
| Grafana | <http://localhost:3000> | `30030` |
| Prometheus | <http://localhost:9090> | `30090` |
| API | <http://localhost:8080> | `30080` |

The values file disables Alertmanager and persistent storage to keep the local
stack small. Metrics are lost when the Prometheus Pod is replaced.

## 1. Install the stack

Requires Helm 3+ and the cluster created with the current `kind.yml`.

```bash
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

helm upgrade --install monitoring prometheus-community/kube-prometheus-stack \
  --version 92.18.0 \
  --namespace monitoring --create-namespace --wait --timeout 15m \
  -f kubernetes/monitoring/prometheus-values.yml
```

Check the Pods:

```bash
kubectl -n monitoring get pods
```

If the cluster was created without the `3000`/`9090` host mappings, use
port-forwards instead:

```bash
kubectl -n monitoring port-forward service/monitoring-grafana 3000:80
kubectl -n monitoring port-forward service/monitoring-kube-prometheus-prometheus 9090:9090
```

## 2. Scrape the API

The API must expose `/metrics`. Rebuild and redeploy it after changing the API
project (the repository image includes the OpenTelemetry instrumentation):

```bash
# Build and load the image with Podman, then restart the Deployment.
podman build -t localhost/round-batman:dev -f Containerfile .
podman save --format docker-archive -o /tmp/round-batman.tar localhost/round-batman:dev
kind load image-archive /tmp/round-batman.tar --name kind
kubectl -n round-batman rollout restart deployment/round-batman-api
kubectl -n round-batman rollout status deployment/round-batman-api --timeout=180s
```

Verify:

```bash
curl -s http://localhost:8080/metrics | head
```

Apply the ServiceMonitor and the dashboard:

```bash
kubectl apply -f kubernetes/monitoring/api-servicemonitor.yml
kubectl apply -f kubernetes/monitoring/round-batman-dashboard.yml
```

Prometheus discovers the `round-batman-api` Service because it has the
`app: round-batman-api` label and the ServiceMonitor is labelled
`release: monitoring`. Check **Status → Targets** in Prometheus; the
`round-batman/round-batman-api` target should be `UP`.

Grafana's dashboard sidecar discovers the dashboard ConfigMap automatically.
Open **Dashboards → Round-Batman / API and database**.

Grafana login for this local setup is `admin` / `admin` (set in
`prometheus-values.yml`). Do not reuse this password outside local development.

## 3. Generate load and watch the graphs

From the repository root:

```bash
~/.venvs/round-batman-load/bin/locust -f load_test/locustfile.py \
  --host http://localhost:8080 \
  --headless --users 5 --spawn-rate 1 --run-time 5m \
  --teams-per-tournament 8 \
  --csv /tmp/round-batman-load --html /tmp/round-batman-load.html
```

Open Grafana with a 5–15 minute time range. The dashboard shows:

- HTTP request rate by route.
- p95 request latency by route.
- 5xx error ratio.
- API Pod CPU and memory.
- Available API replicas (HPA / rollout).
- PostgreSQL Pod CPU and memory.
- Container restarts.
- Npgsql connection pool usage (used, idle, and the configured maximum).

Useful PromQL queries:

```promql
# Request rate by route
sum by (http_route) (
  rate(http_server_request_duration_seconds_count{namespace="round-batman"}[1m])
)

# p95 latency by route
histogram_quantile(0.95,
  sum by (le, http_route) (
    rate(http_server_request_duration_seconds_bucket{namespace="round-batman"}[5m])
  )
)

# 5xx ratio
sum(rate(http_server_request_duration_seconds_count{
  namespace="round-batman", http_response_status_code=~"5.."}[1m]))
/
sum(rate(http_server_request_duration_seconds_count{namespace="round-batman"}[1m]))

# API container CPU
sum by (pod) (rate(container_cpu_usage_seconds_total{
  namespace="round-batman", container!="", pod=~"round-batman-api.*"}[1m]))
```

## Removing the stack

```bash
kubectl delete -f kubernetes/monitoring/round-batman-dashboard.yml
kubectl delete -f kubernetes/monitoring/api-servicemonitor.yml
helm uninstall monitoring -n monitoring
```

Deleting the monitoring namespace also removes the CRDs and any dashboards or
metrics stored there.
