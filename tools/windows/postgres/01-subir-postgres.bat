@echo off
cd /d C:\DBBACK\valoregrouppesquisa
docker compose -f docker-compose.postgres.yml up -d
pause
