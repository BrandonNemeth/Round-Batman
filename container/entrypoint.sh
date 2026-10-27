#!/bin/sh
set -eu

if [ "${RUN_MIGRATIONS:-true}" = "true" ]; then
  connection="${ConnectionStrings__RoundBatman:?ConnectionStrings__RoundBatman is required}"
  migration_attempt=1
  while ! /app/efbundle --connection "$connection"; do
    if [ "$migration_attempt" -ge 30 ]; then
      echo "Database migrations failed after 30 attempts." >&2
      exit 1
    fi
    migration_attempt=$((migration_attempt + 1))
    sleep 2
  done
fi

exec dotnet /app/RoundBatman.Api.dll
