# Demo Valora Insight™ — Teste API

- `surveyId`: `survey_demo_valora_insight`
- token público: `valora-insight-demo-token`

## Validar link
```bash
curl -X POST http://localhost:5080/public/surveys/survey_demo_valora_insight/validate -H "Content-Type: application/json" -d '{"token":"valora-insight-demo-token"}'
```

## Enviar resposta 72/125
Use 25 respostas escala 1–5 somando 72 pontos. O resultado esperado é `Em estruturação`.

## Obter resultado
```bash
curl -X POST http://localhost:5080/public/results/<responseId> -H "Content-Type: application/json" -d '{"resultToken":"<token>"}'
```

## Estado Sprint 8
- Produção permanece segura com `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`.
- API/PostgreSQL ficam disponíveis para homologação local/controlada com `DATA_PROVIDER: 'api'` ou `DATA_PROVIDER: 'hybrid'`.
- Firebase, `firebase-repository.js` e `repository.js` são preservados.
- Frontend não armazena SMTP, segredos de e-mail ou token WhatsApp; comunicação deve passar por Gateway/API.
