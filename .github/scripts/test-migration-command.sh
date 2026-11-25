#!/usr/bin/env bash
set -euo pipefail

# Only the disposable PostgreSQL service declared by _test.yaml is used.
export PGHOST=localhost PGPORT=5432 PGUSER=combat PGDATABASE=postgres
database=combat_migration_smoke
psql -v ON_ERROR_STOP=1 -c "CREATE DATABASE ${database}"
export PGDATABASE="${database}"
psql -v ON_ERROR_STOP=1 -c 'CREATE SCHEMA combat AUTHORIZATION combat'

output="${RUNNER_TEMP}/combat-migration-smoke"
dotnet publish Combat.Presentation/Combat.Presentation.csproj \
  --configuration Debug --no-build --no-restore --output "${output}"

export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=${database};Username=combat;Search Path=combat"
printf 'ci-only-password' > "${output}/password"
export ConnectionStrings__PasswordFile="${output}/password"

timeout 120s dotnet "${output}/Combat.Presentation.dll" --migrate

expected=$(find Combat.Infrastructure/Migrations -name '*.cs' ! -name '*.Designer.cs' ! -name '*Snapshot.cs' | wc -l)
applied=$(psql -At -c 'SELECT count(*) FROM combat."__EFMigrationsHistory"')
test "${expected}" -gt 0
test "${applied}" -eq "${expected}"

# A repeat migration must exit successfully and preserve the same history.
timeout 120s dotnet "${output}/Combat.Presentation.dll" --migrate
test "$(psql -At -c 'SELECT count(*) FROM combat."__EFMigrationsHistory"')" -eq "${applied}"

# Migrate-only must not execute Development seeders.
counts=$(psql -At -v ON_ERROR_STOP=1 <<'SQL'
SELECT format('SELECT count(*) FROM %I.%I;', schemaname, tablename)
FROM pg_tables WHERE schemaname = 'combat' AND tablename <> '__EFMigrationsHistory'
\gexec
SQL
)
if grep -Eq '[1-9]' <<< "${counts}"; then
  echo "Migration command unexpectedly inserted application data." >&2
  exit 1
fi

# Missing mounted credentials must fail instead of reporting success or starting HTTP.
set +e
timeout 15s env ConnectionStrings__PasswordFile="${output}/missing-password" \
  dotnet "${output}/Combat.Presentation.dll" --migrate > "${output}/failure.log" 2>&1
status=$?
set -e
if [[ "${status}" == 0 || "${status}" == 124 ]]; then
  echo "Migration command did not fail promptly for missing credentials." >&2
  exit 1
fi
echo "Migration command passed: apply, repeat, no development seed, failure exit."
