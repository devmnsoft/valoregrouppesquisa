# Demo Valora Insight™ — Teste API/PostgreSQL

- `surveyId`: `demo-valora-insight` (alias UUID seed: `11111111-1111-1111-1111-111111111111`)
- token público: `demo-public-token`
- organização: `Valora Group Demo`
- formulário: `Diagnóstico Valora Insight™`
- dimensões: Cultura e Propósito; Gestão e Governança; Liderança; Pessoas e Talentos; Resultados e Crescimento.
- régua: 25 perguntas, 125 pontos.

## Payload validate

```bash
curl -X POST http://localhost:5080/public/surveys/demo-valora-insight/validate \
  -H "Content-Type: application/json" \
  -d '{"token":"demo-public-token","org":"valora-demo"}'
```

## Payload submit — resultado esperado 72/125

```bash
curl -X POST http://localhost:5080/public/surveys/demo-valora-insight/responses \
  -H "Content-Type: application/json" \
  -d '{"token":"demo-public-token","participant":{"name":"Participante Demo","email":"demo@example.test"},"answers":{"q1":3,"q2":3,"q3":3,"q4":3,"q5":3,"q6":3,"q7":3,"q8":3,"q9":3,"q10":3,"q11":3,"q12":3,"q13":3,"q14":3,"q15":3,"q16":3,"q17":3,"q18":3,"q19":3,"q20":3,"q21":3,"q22":3,"q23":2,"q24":2,"q25":2},"lgpdConsent":true,"communicationConsent":true}'
```

## Payload result

```bash
curl -X POST http://localhost:5080/public/results/<responseId> \
  -H "Content-Type: application/json" \
  -d '{"resultToken":"<resultToken>"}'
```

## Resultado esperado

- `totalScore`: `72`
- `maxScore`: `125`
- `maturityLabel`: `Em estruturação`
- certificado: metadata `metadata-ready`
- e-mail: `pending`, sem bloquear a visualização do resultado.
