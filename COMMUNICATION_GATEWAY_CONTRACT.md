# Contrato — Valora Communication Gateway

Base URL produção: `https://api.valoragroup.mnsoft.com.br`.

## Segurança
Todas as rotas `/communication/*` exigem `Authorization: Bearer <GATEWAY_API_TOKEN>` e `Content-Type: application/json` nos métodos POST. CORS aceita somente origens configuradas em `ALLOWED_ORIGINS`.

## Endpoints
- `GET /health`: status público do serviço.
- `GET /communication/status`: status dos provedores SMTP e WhatsApp Cloud API.
- `POST /communication/email/send`: e-mail genérico/manual do admin.
- `POST /communication/whatsapp/send`: mensagem WhatsApp transacional.
- `POST /communication/result/send`: envio principal após conclusão da pesquisa.

## Payload principal
Use `eventType`, `responseId`, `surveyId`, `companyId`, `participant`, `company`, `survey`, `result`, `links` e `channels`. O gateway bloqueia payload sem `responseId`, sem participante ou sem e-mail/telefone válido.

## Status possíveis
`sent`, `disabled`, `not_configured`, `invalid_phone`, `failed`, `rate_limited`, `missing-recipient`.

## Comportamento do frontend
Se o gateway falhar, a conclusão da pesquisa deve continuar, o resultado deve ser exibido, a comunicação deve ser marcada como pendente/falha e o botão manual de WhatsApp deve permanecer disponível.
