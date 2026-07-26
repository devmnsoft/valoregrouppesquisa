@echo off
setlocal
cd /d %~dp0\..\..\..
echo Valora Pulse - PostgreSQL transition helper
docker compose -f docker-compose.postgres.yml up -d
