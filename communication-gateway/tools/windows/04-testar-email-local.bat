@echo off
curl -X POST http://localhost:8097/communication/result/send ^
 -H "Content-Type: application/json" ^
 -d "{\"responseId\":\"resp_teste\",\"resultToken\":\"token_publico\",\"channels\":{\"email\":true,\"whatsapp\":false}}"
pause
