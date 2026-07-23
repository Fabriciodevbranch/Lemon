#!/bin/sh
set -eu

until psql -v ON_ERROR_STOP=1 -c 'SELECT 1 FROM "Users" LIMIT 1' >/dev/null 2>&1; do
  sleep 2
done

psql -v ON_ERROR_STOP=1 -f /seed/001-seed-admin.sql
psql -v ON_ERROR_STOP=1 -v grafana_password="$GRAFANA_DB_PASSWORD" -f /seed/002-grafana-reader.sql
