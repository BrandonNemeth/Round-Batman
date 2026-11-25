#!/usr/bin/env python3
"""Interactive local development launcher for Round-Batman."""

from __future__ import annotations

import base64
import getpass
import json
import os
import shutil
import signal
import socket
import subprocess
import sys
import tempfile
import time
from pathlib import Path


ROOT = Path(__file__).resolve().parent
NAMESPACE = "round-batman"
KIND_CONTEXT = "kind-kind"
KIND_CLUSTER = "kind"
IMAGE = "localhost/round-batman:dev"
UI_PORT = 5500
API_PORT = 8080


def command(
    args: list[str],
    *,
    input_text: str | None = None,
    check: bool = True,
    capture_output: bool = False,
):
    """Run a command from the project root and report missing tools clearly."""
    try:
        return subprocess.run(
            args,
            cwd=ROOT,
            input=input_text,
            text=True,
            check=check,
            capture_output=capture_output,
        )
    except FileNotFoundError as error:
        raise RuntimeError(f"Required command not found: {args[0]}") from error
    except subprocess.CalledProcessError as error:
        raise RuntimeError(f"Command failed (exit {error.returncode}): {' '.join(args)}") from error


def ask_choice(prompt: str, choices: list[str]) -> int:
    print(f"\n{prompt}")
    for index, choice in enumerate(choices, start=1):
        print(f"  {index}. {choice}")
    while True:
        value = input("> ").strip()
        if value.isdigit() and 1 <= int(value) <= len(choices):
            return int(value)
        print(f"Choose a number from 1 to {len(choices)}.")


def ask_value(prompt: str, default: str, validator=None) -> str:
    while True:
        value = input(f"{prompt} [{default}]: ").strip() or default
        if validator is None or validator(value):
            return value
        print("Please enter a valid value.")


def read_env_password() -> str | None:
    value = os.environ.get("POSTGRES_PASSWORD")
    if value:
        return value

    env_file = ROOT / ".env"
    if not env_file.exists():
        return None
    for line in env_file.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        key, value = line.split("=", 1)
        if key.strip().removeprefix("export ").strip() != "POSTGRES_PASSWORD":
            continue
        value = value.strip()
        if len(value) >= 2 and value[0] == value[-1] and value[0] in "\"'":
            quote = value[0]
            value = value[1:-1]
            if quote == "'":
                value = value.replace("\\'", "'").replace("\\\\", "\\")
        return value
    return None


def dotenv_value(value: str) -> str:
    """Quote a Compose dotenv value so special characters remain literal."""
    return "'" + value.replace("\\", "\\\\").replace("'", "\\'") + "'"


def ensure_password() -> str:
    password = read_env_password()
    if password:
        return password

    print("\nRound-Batman needs a PostgreSQL password.")
    print("It will be saved in the ignored .env file with owner-only permissions.")
    while True:
        password = getpass.getpass("PostgreSQL password: ")
        confirmation = getpass.getpass("Confirm password: ")
        if not password:
            print("The password cannot be empty.")
        elif password != confirmation:
            print("Passwords did not match.")
        elif "\n" in password or "\r" in password:
            print("Newlines are not supported in the password.")
        else:
            break

    env_file = ROOT / ".env"
    old_contents = env_file.read_text(encoding="utf-8") if env_file.exists() else ""
    lines = []
    for line in old_contents.splitlines():
        assignment = line.strip().removeprefix("export ")
        if assignment.split("=", 1)[0].strip() != "POSTGRES_PASSWORD":
            lines.append(line)
    lines.append(f"POSTGRES_PASSWORD={dotenv_value(password)}")
    env_file.write_text("\n".join(lines) + "\n", encoding="utf-8")
    env_file.chmod(0o600)
    return password


def kubectl(*args: str, input_text: str | None = None, check: bool = True):
    return command(
        ["kubectl", "--context", KIND_CONTEXT, *args],
        input_text=input_text,
        check=check,
    )


def ensure_kind_cluster() -> None:
    result = command(["kind", "get", "clusters"], check=False, capture_output=True)
    clusters = result.stdout.splitlines() if result.stdout else []
    if KIND_CLUSTER not in clusters:
        print("Creating the local kind cluster...")
        command(["kind", "create", "cluster", "--name", KIND_CLUSTER, "--config", "kind.yml"])
    else:
        print(f"Using existing kind cluster '{KIND_CLUSTER}'.")


def apply_kind_secret(password: str) -> None:
    encoded_password = base64.b64encode(password.encode("utf-8")).decode("ascii")
    manifest = json.dumps(
        {
            "apiVersion": "v1",
            "kind": "Secret",
            "metadata": {"name": "round-batman-db-credentials", "namespace": NAMESPACE},
            "type": "Opaque",
            "data": {"password": encoded_password},
        }
    )
    # Send the secret over stdin rather than exposing it in argv or a tracked YAML file.
    kubectl("apply", "-f", "-", input_text=manifest)


def build_and_load_api() -> None:
    command(["podman", "build", "-f", "Containerfile", "-t", IMAGE, "."])
    with tempfile.NamedTemporaryFile(prefix="round-batman-", suffix=".tar", delete=False) as archive_file:
        archive = Path(archive_file.name)
    try:
        command(["podman", "save", "--format", "docker-archive", "-o", str(archive), IMAGE])
        command(["kind", "load", "image-archive", str(archive), "--name", KIND_CLUSTER])
    finally:
        archive.unlink(missing_ok=True)


def start_kind(components: str) -> None:
    ensure_kind_cluster()
    password = ensure_password()
    kubectl("apply", "-f", "kubernetes/namespace.yml")
    apply_kind_secret(password)

    if components in ("all", "database", "api"):
        kubectl("apply", "-f", "database/postgres.yml")
        print("Waiting for PostgreSQL...")
        kubectl("-n", NAMESPACE, "rollout", "status", "statefulset/round-batman-db", "--timeout=180s")

    if components in ("all", "api"):
        build_and_load_api()
        kubectl("apply", "-f", "kubernetes/app-config.yml")
        kubectl("apply", "-f", "kubernetes/api.yml")
        print("Waiting for the API...")
        kubectl("-n", NAMESPACE, "rollout", "status", "deployment/round-batman-api", "--timeout=180s")
        print(f"API: http://localhost:{API_PORT} (health: /health/db)")


def stop_kind(components: str) -> None:
    resource_names = {
        "all": ["deployment/round-batman-api", "statefulset/round-batman-db"],
        "api": ["deployment/round-batman-api"],
        "database": ["statefulset/round-batman-db"],
    }[components]
    for resource in resource_names:
        result = kubectl(
            "-n", NAMESPACE, "get", resource, "--ignore-not-found", "-o", "name",
            check=False, capture_output=True,
        )
        if result.stdout.strip():
            kubectl("-n", NAMESPACE, "scale", "--replicas=0", resource)
    print("Scaled down. Persistent database storage and the kind cluster were kept.")


def compose_command(*args: str, check: bool = True):
    # Compose interpolates the required password even for read-only/status commands.
    env = os.environ.copy()
    env.setdefault("POSTGRES_PASSWORD", read_env_password() or "unused-for-compose-command")
    try:
        return subprocess.run(
            ["podman", "compose", *args], cwd=ROOT, env=env, check=check
        )
    except FileNotFoundError as error:
        raise RuntimeError("Required command not found: podman") from error
    except subprocess.CalledProcessError as error:
        raise RuntimeError(f"Podman Compose failed (exit {error.returncode}).") from error


def start_compose(components: str) -> None:
    password = ensure_password()
    # Compose reads .env itself. Export the already configured password for this run
    # too, in case the value came from the shell rather than the file.
    env = os.environ.copy()
    env["POSTGRES_PASSWORD"] = password
    try:
        subprocess.run(
            ["podman", "compose", "up", "-d", *compose_components(components)],
            cwd=ROOT,
            env=env,
            check=True,
        )
    except FileNotFoundError as error:
        raise RuntimeError("Required command not found: podman") from error
    except subprocess.CalledProcessError as error:
        raise RuntimeError(f"Podman Compose failed (exit {error.returncode}).") from error
    if components != "database":
        print(f"API: http://localhost:{API_PORT} (health: /health/db)")


def compose_components(components: str) -> list[str]:
    if components == "all":
        return []
    if components == "database":
        return ["postgres"]
    return ["api"]  # Compose starts its declared postgres dependency as needed.


def stop_compose(components: str) -> None:
    if components == "all":
        compose_command("down")  # Does not remove the named database volume.
    else:
        service = "postgres" if components == "database" else "api"
        compose_command("stop", service)
    print("Stopped. The named database volume was preserved.")


def show_status(runtime: str) -> None:
    if runtime == "kind":
        kubectl("-n", NAMESPACE, "get", "pods,services,pvc", check=False)
    else:
        compose_command("ps", check=False)


def recent_matches_sql(limit: int) -> str:
    return f'''SELECT
    tournament."Name" AS "Tournament",
    tournament."Id" AS "TournamentId",
    m."Id" AS "MatchId",
    m."CompletedAt" AS "CompletedAt",
    m."Stage" AS "Stage",
    m."Round" AS "Round",
    home."Name" AS "HomeTeam",
    m."Score_HomeTeamScore" AS "HomeScore",
    visitor."Name" AS "VisitorTeam",
    m."Score_VisitorTeamScore" AS "VisitorScore",
    CASE
        WHEN m."Score_HomeTeamScore" > m."Score_VisitorTeamScore" THEN home."Name"
        WHEN m."Score_HomeTeamScore" < m."Score_VisitorTeamScore" THEN visitor."Name"
        ELSE 'Draw'
    END AS "Winner"
FROM "Matches" AS m
JOIN "Tournaments" AS tournament ON tournament."Id" = m."TournamentId"
JOIN "Teams" AS home ON home."Id" = m."HomeTeamId"
JOIN "Teams" AS visitor ON visitor."Id" = m."VisitorTeamId"
WHERE m."CompletedAt" IS NOT NULL
ORDER BY m."CompletedAt" DESC, m."Id" ASC
LIMIT {limit};'''


def show_recent_matches() -> None:
    limit = int(ask_value(
        "Number of most recently completed matches",
        "20",
        lambda value: value.isdigit() and 1 <= int(value) <= 10000,
    ))
    runtime = choose_runtime()
    sql = recent_matches_sql(limit)
    print("\nSQL to be executed:\n")
    print(sql)
    if input("Run this query? [Y/n]: ").strip().lower() not in ("", "y", "yes"):
        print("Query cancelled.")
        return

    if runtime == "kind":
        kubectl(
            "-n", NAMESPACE, "exec", "round-batman-db-0", "--",
            "psql", "-X", "-U", "roundbatman", "-d", "roundbatman",
            "-P", "pager=off", "-c", sql,
        )
    else:
        compose_command(
            "exec", "-T", "postgres", "psql", "-X", "-U", "roundbatman",
            "-d", "roundbatman", "-P", "pager=off", "-c", sql,
        )


def ui_state_dir() -> Path:
    state_root = Path(os.environ.get("XDG_STATE_HOME", Path.home() / ".local/state"))
    directory = state_root / "round-batman"
    directory.mkdir(parents=True, exist_ok=True)
    return directory


def ui_state_file() -> Path:
    return ui_state_dir() / "web-ui.json"


def start_ui() -> None:
    state_file = ui_state_file()
    if state_file.exists():
        print("A Web UI process is already recorded. Try Stop Web UI first.")
        return

    port = int(ask_value("Web UI port", str(UI_PORT), lambda value: value.isdigit() and 1 <= int(value) <= 65535))
    log_path = ui_state_dir() / "web-ui.log"
    log_file = log_path.open("a", encoding="utf-8")
    try:
        process = subprocess.Popen(
            [sys.executable, "-m", "http.server", str(port), "--bind", "127.0.0.1", "--directory", str(ROOT / "client")],
            cwd=ROOT,
            stdin=subprocess.DEVNULL,
            stdout=log_file,
            stderr=subprocess.STDOUT,
            start_new_session=True,
        )
    except OSError as error:
        log_file.close()
        raise RuntimeError(f"Could not start the Web UI: {error}") from error
    log_file.close()

    for _ in range(20):
        if process.poll() is not None:
            raise RuntimeError(f"Web UI exited early; see {log_path}")
        with socket.socket() as connection:
            if connection.connect_ex(("127.0.0.1", port)) == 0:
                break
        time.sleep(0.1)
    else:
        process.terminate()
        raise RuntimeError(f"Web UI did not open port {port}; see {log_path}")

    state_file.write_text(json.dumps({"pid": process.pid, "port": port}), encoding="utf-8")
    print(f"Web UI: http://localhost:{port}")
    print(f"For kind/Compose, set its 'URL de la API' field to http://localhost:{API_PORT}.")
    print(f"Log: {log_path}")


def process_command_line(pid: int) -> str | None:
    proc_cmdline = Path(f"/proc/{pid}/cmdline")
    if proc_cmdline.exists():
        try:
            return proc_cmdline.read_bytes().replace(b"\0", b" ").decode(errors="replace")
        except OSError:
            return None
    result = subprocess.run(["ps", "-p", str(pid), "-o", "args="], capture_output=True, text=True, check=False)
    return result.stdout.strip() if result.returncode == 0 else None


def stop_ui() -> None:
    state_file = ui_state_file()
    if not state_file.exists():
        print("No Web UI process is recorded as running.")
        return
    try:
        state = json.loads(state_file.read_text(encoding="utf-8"))
        pid = int(state["pid"])
        port = int(state["port"])
    except (OSError, ValueError, KeyError, json.JSONDecodeError):
        state_file.unlink(missing_ok=True)
        print("Removed an invalid Web UI process record.")
        return

    cmdline = process_command_line(pid)
    if (
        not cmdline
        or "-m http.server" not in cmdline
        or str(port) not in cmdline
        or str(ROOT / "client") not in cmdline
    ):
        state_file.unlink(missing_ok=True)
        print("The recorded Web UI is no longer running; removed its stale process record.")
        return
    try:
        os.kill(pid, signal.SIGTERM)
    except ProcessLookupError:
        pass
    state_file.unlink(missing_ok=True)
    print("Web UI stopped.")


def locust_executable() -> str:
    configured = Path.home() / ".venvs/round-batman-load/bin/locust"
    if configured.is_file():
        return str(configured)
    installed = shutil.which("locust")
    if installed:
        return installed
    raise RuntimeError(
        "Locust is not installed. Run: python3 -m venv ~/.venvs/round-batman-load "
        "&& ~/.venvs/round-batman-load/bin/pip install -r load_test/requirements.txt"
    )


def run_locust() -> None:
    runtime = choose_runtime()
    default_host = f"http://localhost:{API_PORT}"
    host = ask_value("API base URL", default_host)
    tournament_choice = ask_choice(
        "Tournament format for the Locust scenario",
        ["Pure round robin", "Round robin + knockout (hybrid)"],
    )
    tournament_type = "ROUND_ROBIN" if tournament_choice == 1 else "HYBRID"
    mode = ask_choice("Locust mode", ["Interactive Web UI", "Headless run"])
    args = [
        locust_executable(), "-f", "load_test/locustfile.py", "--host", host,
        "--tournament-type", tournament_type,
    ]

    if mode == 1:
        print("Locust UI: http://localhost:8089 (Ctrl+C in this terminal to stop).")
    else:
        users = ask_value("Users", "5", lambda value: value.isdigit() and int(value) > 0)
        spawn_rate = ask_value(
            "Spawn rate (users/second)", "1",
            lambda value: is_positive_number(value),
        )
        duration = ask_value("Run time (e.g. 2m, 30s)", "2m", lambda value: bool(value))
        team_count = ask_value(
            "Teams per tournament (2-32)", "8",
            lambda value: value.isdigit() and 2 <= int(value) <= 32,
        )
        args.extend(["--headless", "--users", users, "--spawn-rate", spawn_rate,
                     "--run-time", duration, "--teams-per-tournament", team_count])
        csv_prefix = input("Optional CSV/HTML output prefix (Enter to skip): ").strip()
        if csv_prefix:
            args.extend(["--csv", csv_prefix, "--html", f"{csv_prefix}.html"])

    print(f"Target ({runtime}): {host}")
    command(args)


def is_positive_number(value: str) -> bool:
    try:
        return float(value) > 0
    except ValueError:
        return False


def choose_runtime() -> str:
    choice = ask_choice("Environment", ["Kubernetes (kind)", "Podman Compose"])
    return "kind" if choice == 1 else "compose"


def choose_components() -> str:
    choice = ask_choice("Components", ["Database + API", "Database only", "API (starts database too)"])
    return {1: "all", 2: "database", 3: "api"}[choice]


def app_action(start: bool) -> None:
    runtime = choose_runtime()
    components = choose_components()
    if runtime == "kind":
        (start_kind if start else stop_kind)(components)
    elif start:
        start_compose(components)
    else:
        stop_compose(components)


def main() -> int:
    print("Round-Batman local development CLI")
    while True:
        try:
            choice = ask_choice(
                "What would you like to do?",
                [
                    "Start application",
                    "Stop application",
                    "Show status",
                    "Start Web UI",
                    "Stop Web UI",
                    "Show recent matches (SQL preview)",
                    "Run Locust",
                    "Exit",
                ],
            )
            if choice == 1:
                app_action(start=True)
            elif choice == 2:
                app_action(start=False)
            elif choice == 3:
                runtime = choose_runtime()
                show_status(runtime)
            elif choice == 4:
                start_ui()
            elif choice == 5:
                stop_ui()
            elif choice == 6:
                show_recent_matches()
            elif choice == 7:
                run_locust()
            else:
                return 0
        except KeyboardInterrupt:
            print("\nInterrupted.")
        except (RuntimeError, OSError, ValueError) as error:
            print(f"\nError: {error}", file=sys.stderr)


if __name__ == "__main__":
    raise SystemExit(main())
