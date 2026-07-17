#!/bin/sh
set -eu

until psql -v ON_ERROR_STOP=1 -c 'SELECT 1 FROM "Users" LIMIT 1' >/dev/null 2>&1; do
  sleep 2
done

psql -v ON_ERROR_STOP=1 -f /seed/001-seed-admin.sql
