@echo off
curl -X POST http://localhost:8097/communication/email/send -H "Authorization: Bearer %GATEWAY_API_TOKEN%" -H "Content-Type: application/json" -d "{\"to\":\"teste@exemplo.com\",\"subject\":\"Teste Valora\",\"text\":\"Teste SMTP\"}"
