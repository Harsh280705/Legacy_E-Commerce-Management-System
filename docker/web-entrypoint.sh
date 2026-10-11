#!/bin/sh
# Web entrypoint: inject DB settings into Web.config, then start XSP4.
# Env (from compose/.env): DB_HOST DB_PORT DB_NAME DB_USER DB_PASSWORD
set -e
DB_HOST="${DB_HOST:-db}"
DB_PORT="${DB_PORT:-1433}"
DB_NAME="${DB_NAME:-LegacyEcommerceDb}"
DB_USER="${DB_USER:-sa}"
DB_PASSWORD="${DB_PASSWORD:-}"

if [ -z "$DB_PASSWORD" ]; then
  echo "ERROR: DB_PASSWORD is not set." >&2
  exit 1
fi

CS="Server=${DB_HOST},${DB_PORT};Database=${DB_NAME};User Id=${DB_USER};Password=${DB_PASSWORD};Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;"
# Replace connection string in Web.config (python3 may not exist; mono image has python? use sed with escaped string).
# Passwords are URL-safe base64-ish in our setup; escape & and / for sed.
ESC_CS=$(printf '%s' "$CS" | sed -e 's/[\/&]/\\&/g')
sed -i "s/Server=db,1433;Database=LegacyEcommerceDb;User Id=sa;Password=__DB_PASSWORD__;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;/${ESC_CS}/" /app/Web.config

echo "Starting XSP4 on :8080 (db=${DB_HOST},${DB_PORT}/${DB_NAME})..."
exec xsp4 --port 8080 --address 0.0.0.0 --root /app --nonstop
