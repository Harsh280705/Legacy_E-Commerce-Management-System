#!/bin/sh
# One-shot DB init: wait for SQL Server, then apply schema + seed idempotently.
set -e
HOST="${DB_HOST:-db}"
PORT="${DB_PORT:-1433}"
USER="${DB_USER:-sa}"
PASS="${DB_PASSWORD:-}"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd -S ${HOST},${PORT} -U ${USER} -P ${PASS} -C -l 30"

echo "Waiting for SQL Server at ${HOST},${PORT}..."
for i in $(seq 1 60); do
  if $SQLCMD -Q "SELECT 1" -b > /dev/null 2>&1; then
    echo "SQL Server is up."
    break
  fi
  if [ "$i" -eq 60 ]; then echo "ERROR: SQL Server never became ready." >&2; exit 1; fi
  sleep 5
done

run() {
  echo "Applying $1..."
  if ! $SQLCMD -i "/db/$1" -b; then
    echo "ERROR: failed applying $1" >&2
    exit 1
  fi
}

run schema.sql
if [ "${SEED_SIZE:-100}" = "1000" ]; then
  run seed_1000.sql
else
  run seed.sql
fi
echo "DB init complete."
