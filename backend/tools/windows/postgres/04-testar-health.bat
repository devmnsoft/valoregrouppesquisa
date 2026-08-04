@echo off
setlocal
cd /d %~dp0\..\..\..
echo Valora Insight™ - PostgreSQL transition helper
curl http://localhost:5080/health
curl http://localhost:5080/health/database
curl http://localhost:5080/health/config
