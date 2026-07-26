@echo off
setlocal
cd /d %~dp0\..\..\..
echo Valora Pulse - PostgreSQL transition helper
curl -X POST http://localhost:5080/admin/database/migrate
