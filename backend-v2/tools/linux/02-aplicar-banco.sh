#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."
psql "${POSTGRES_CONNECTION:-postgresql://postgres:postgres@localhost:5432/valorapesquisa}" -f database/postgresql/scriptbd_completo.sql
