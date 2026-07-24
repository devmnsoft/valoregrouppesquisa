# Database Script Sync
`database/migrate.sql` é o fluxo oficial e usa `\i database/migrations/NNN_nome.sql` para 001-029. `028_client_onboarding.sql` está incluída antes de `029_operational_completeness_v61.sql`. Valide com `pwsh scripts/qa/check-database-scripts.ps1` e `psql -U postgres -d habitflow -f database/validate_schema_habitflow.sql`.
