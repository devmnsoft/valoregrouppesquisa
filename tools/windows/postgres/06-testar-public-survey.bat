@echo off
setlocal
cd /d %~dp0\..\..\..
echo Valora Pulse - PostgreSQL transition helper
curl -X POST http://localhost:5080/public/surveys/00000000-0000-0000-0000-000000000000/validate -H "Content-Type: application/json" -d "{\"token\":\"token-publico\",\"org\":\"slug\"}"
