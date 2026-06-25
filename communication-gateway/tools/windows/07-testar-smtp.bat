@echo off
cd /d C:\DBBACK\valoregrouppesquisa\communication-gateway
curl -X POST http://127.0.0.1:8097/communication/email/test -H "Content-Type: application/json" -d "{\"to\":\"teste@email.com\"}"
pause
