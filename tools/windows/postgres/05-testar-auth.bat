@echo off
curl -X POST http://localhost:5080/auth/register-company -H "Content-Type: application/json" -d "{\"name\":\"Marcelo\",\"companyName\":\"Empresa Cliente\",\"email\":\"cliente@email.com\",\"password\":\"123456\"}"
curl -X POST http://localhost:5080/auth/login -H "Content-Type: application/json" -d "{\"email\":\"cliente@email.com\",\"password\":\"123456\"}"
pause
