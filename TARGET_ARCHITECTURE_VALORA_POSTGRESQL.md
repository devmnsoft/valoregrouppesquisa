# Arquitetura Alvo — Valora Pulse™ + PostgreSQL

## Frontend
HTML/CSS/JavaScript atual, publicável no IIS. Não deve conter senha SMTP, token WhatsApp nem ser fonte final de cálculo de resultado em produção. Consome backend via `api-client.js`.

## Backend
ASP.NET Core API com PostgreSQL, Dapper, JWT/Auth, BCrypt, Swagger, logging estruturado, serviços de plano, pesquisa, resposta, resultado, certificado, comunicação e auditoria centralizada.

## Banco
PostgreSQL relacional com schemas `valora`, `billing`, `communication` e `audit`, integridade referencial, histórico, relatórios e controle mensal de uso dos planos.

## Comunicação
O `communication-gateway` pode continuar como serviço externo na transição. Depois, envio e filas podem ser internalizados na API.

## Modos oficiais
```js
DATA_PROVIDER: 'firebase' | 'api' | 'hybrid'
```
- `firebase`: sistema atual e modo seguro de produção durante transição.
- `api`: backend PostgreSQL como fonte principal.
- `hybrid`: leitura/comparação controlada entre Firebase e API.
