# Round-Batman API load test (Locust)

Each simulated user runs an independent, complete tournament lifecycle with
unique resource names. Choose `ROUND_ROBIN` for a pure round robin or `HYBRID`
for the round-robin-plus-knockout format. The default is `HYBRID`. A scenario
covers every implemented API route:

- `GET /health` and `GET /health/db`.
- Team create, read, update, list, delete, and `POST /teams/generate`.
- Tournament create, read, update (`PUT`), patch (`PATCH`), list, and delete.
- Group create, read, update, list, and delete.
- Team assignment through `PATCH .../teams`, followed by verification of the
  persisted membership.
- Round-robin match generation, match read/list, score update, explicit match
  create/delete, and `POST .../matches/simulate-results`.
- End-to-end verification that the tournament reaches `FINISHED` with a
  champion; hybrid runs must include knockout matches and pure round robin runs
  must not.
- `GET .../matches/recent` returns the 20 most recently completed matches (or all
  completed matches when fewer than 20 exist), newest first, with completion
  timestamps. The full match history remains available from `GET .../matches`.

The script groups statistics by route template rather than individual UUIDs,
validates status codes, IDs, `Location` headers, and response content, and
aborts a scenario at its first failure. `SCENARIO Full API lifecycle` reports
end-to-end success and latency in addition to the individual HTTP request
statistics.

## Install

Run from the repository root. Python 3.10+ is recommended. Create the virtual
environment outside the repository:

```bash
python3 -m venv ~/.venvs/round-batman-load
~/.venvs/round-batman-load/bin/python -m pip install -r load_test/requirements.txt
```

## Start the application first

Use the main README to start PostgreSQL and the API in kind, or use
`compose.yaml`. The normal local base URL is <http://localhost:8080>.

For the current kind deployment:

```bash
curl -s http://localhost:8080/health
curl -s http://localhost:8080/health/db
```

Both must succeed before starting Locust. Do not run the load test against a
database whose data you need to keep.

## Run interactively

```bash
~/.venvs/round-batman-load/bin/locust -f load_test/locustfile.py \
  --host http://localhost:8080 --tournament-type HYBRID
```

Open <http://localhost:8089>, start with a small user count, and watch the
charts while the scenarios run. Each user waits 1–3 seconds between complete
scenarios; requests inside a scenario run in sequence.

## Run headless

```bash
~/.venvs/round-batman-load/bin/locust -f load_test/locustfile.py \
  --host http://localhost:8080 --tournament-type HYBRID \
  --headless --users 5 --spawn-rate 1 --run-time 2m \
  --teams-per-tournament 8 \
  --csv /tmp/round-batman-load --html /tmp/round-batman-load.html
```

`--tournament-type` accepts `ROUND_ROBIN` (pure) or `HYBRID` and defaults to
`HYBRID`. `--teams-per-tournament` must be between 2 and 32. The generated group
schedule contains `teams * (teams - 1) / 2` matches, so larger values produce
significantly more work per scenario.

`--keep-data` keeps the main tournament and its teams after each scenario. The
default is to delete everything the scenario created (the explicit-match
tournament is always cleaned up). The bulk-generated smoke team is also
deleted immediately.

A single scenario performs roughly:

- 2 health reads.
- 2 team CRUD sequences (one explicit team and one generated team).
- `teams-per-tournament + 5` team requests.
- 18 tournament/group/match requests, including generation, scoring,
  simulation, and cleanup.
- One synthetic `SCENARIO` sample.

## Data and interpretation

- Each user owns all of its resources; no user mutates another user's
  tournament. This measures independent workflows, not contention on one
  tournament.
- The default cleanup makes repeated benchmark runs comparable. With
  `--keep-data`, `GET /teams` and `GET /tournaments` grow over time and their
  cost increases.
- `POST /teams/generate` creates globally unique team names by checking the
  current table. Very high concurrency can race on the unique name index; a
  failure there is an application/database finding, not a Locust bug.
- The synthetic `SCENARIO` samples contribute to Locust's aggregate
  statistics. Filter by request type when calculating HTTP-only throughput.
- Importing the script does not generate load.

## Monitoring the run

The repository includes a local Prometheus/Grafana setup in
[`kubernetes/monitoring`](../kubernetes/monitoring/README.md). With the current
`kind.yml` port mappings:

| Tool | URL |
| --- | --- |
| Grafana | <http://localhost:3000> |
| Prometheus | <http://localhost:9090> |
| API | <http://localhost:8080> |

Grafana's **Round-Batman / API and database** dashboard shows request rate,
p95 latency, error rate, pod CPU/memory, replica count, and Npgsql connection
pool usage. Run the headless Locust command and open the dashboard with the
time range set to the last 15 minutes.

Useful PromQL checks during a run:

```promql
sum by (http_route) (rate(http_server_request_duration_seconds_count[1m]))

histogram_quantile(0.95,
  sum by (le, http_route) (rate(http_server_request_duration_seconds_bucket[5m])))

sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[1m]))
  / sum(rate(http_server_request_duration_seconds_count[1m]))
```

## Script tests

After installing dependencies:

```bash
~/.venvs/round-batman-load/bin/python -m py_compile load_test/locustfile.py
```

The optional offline contract tests can be run with:

```bash
~/.venvs/round-batman-load/bin/python -m unittest discover -s load_test -p 'test_*.py'
```
