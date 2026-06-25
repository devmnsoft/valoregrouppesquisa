@echo off
cd /d C:\DBBACK\valoregrouppesquisa\communication-gateway
curl -X POST http://127.0.0.1:8097/communication/queue/process -H "Content-Type: application/json" -d "{}"
pause
