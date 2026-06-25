@echo off
cd /d C:\DBBACK\valoregrouppesquisa\communication-gateway
curl -X POST http://127.0.0.1:8097/communication/result/send -H "Content-Type: application/json" -d "{\"responseId\":\"resp_123\",\"resultToken\":\"token_do_resultado\",\"channels\":{\"email\":true}}"
pause
