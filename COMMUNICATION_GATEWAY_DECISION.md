# Decisão do Communication Gateway

## Opções
- Opção A: manter `communication-gateway` temporariamente para e-mail e status de comunicação.
- Opção B: absorver e-mail na API ASP.NET Core.

## Recomendação Sprint 8
Manter compatibilidade com gateway, mas preparar a API ASP.NET Core para assumir e-mail pelos endpoints `POST /communications/result/{responseId}/send-email` e `GET /communications`.

## Regras
- Frontend nunca envia SMTP.
- Frontend nunca guarda segredo de e-mail.
- Frontend nunca guarda token WhatsApp.
- Gateway/API validam dados no servidor.
- Falha de e-mail não bloqueia resultado.

## Endpoints esperados do gateway
- `GET /health`
- `GET /communication/status`
- `POST /communication/result/send`
- `POST /communication/email/test`

## Estado Sprint 8
- Produção permanece segura com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`.
- API/PostgreSQL ficam disponíveis para homologação local/controlada com `DATA_PROVIDER: 'api'` ou `DATA_PROVIDER: 'hybrid'`.
- Firebase, `firebase-repository.js` e `repository.js` são preservados.
- Frontend não armazena SMTP, segredos de e-mail ou token WhatsApp; comunicação deve passar por Gateway/API.
