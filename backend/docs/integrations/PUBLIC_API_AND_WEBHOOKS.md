# Integrações profissionais — API, webhooks e BI

## API Keys

Em **Integrações > API Keys**, uma pessoa com `api_keys.manage` informa nome,
escopos e validade. O token `vli_…` aparece somente na resposta de criação; a
persistência recebe apenas SHA-256, prefixo, organização e metadados. Guarde-o
em um cofre. Rotação revoga a chave anterior imediatamente.

Envie `X-API-Key: vli_...` (preferencial) ou `Authorization: Bearer vli_...`.
Uma chave expirada/revogada recebe `401`; escopo insuficiente recebe `403`; um
objeto de outro tenant é indistinguível de inexistente (`404`). O limite padrão
é 120 requisições por chave/minuto.

## Endpoints e escopos

| Endpoint | Escopo |
|---|---|
| `GET /api/public/v1/organizations/{id}/summary` | `organizations.read` |
| `GET /api/public/v1/diagnostics/{id}/summary`, `/scores`, `/dimensions` | `diagnostics.read` |
| `GET /api/public/v1/reports/{id}/metadata` | `reports.read` |
| `GET /api/public/v1/certificates/{code}/validation` | `certificates.validate` |
| `GET /api/public/v1/benchmark/{id}` | `benchmark.read` |
| `GET /api/public/v1/evolution/{organizationId}` | `evolution.read` |

Exemplo: `curl -H 'X-API-Key: vli_***' https://host/api/public/v1/organizations/UUID/summary`.
Respostas são metadados/agregados: e-mail, documento, token e respostas
individuais não integram o contrato público.

## Webhooks

Eventos: `diagnosis.created`, `diagnosis.published`, `response.received`,
`diagnosis.completed`, `report.generated`, `certificate.issued`,
`action.created`, `action.completed`, `subscription.updated` e
`usage.limit_reached`. A URL deve ser HTTPS.

O corpo é um envelope `{ id, type, organizationId, occurredAt, data }`. A
assinatura é `sha256=` + HMAC-SHA256 do corpo JSON exato e segue no header
`X-Valora-Signature`. Compare em tempo constante. Entregas falhas ficam na fila
com backoff exponencial, no máximo seis tentativas, e podem ser reenviadas sem
bloquear a operação que originou o evento.

## BI, cadastro, e-mail e importações

O dataset BI é preparado para `organizations`, `diagnostics`,
`responses_summary`, `scores`, `dimensions`, `concepts`, `evidence_items`,
`action_plans`, `certificates`, `evolution_snapshots` e `benchmark_snapshots`.
Exportações exigem `bi.read` e nunca incluem PII sem autorização explícita.
CNPJ/CEP usam providers configuráveis e retornam fallback manual quando
indisponíveis. E-mails são gravados em `email_outbox`, com template, retry e
configuração de SMTP por ambiente. CSV externo passa por pré-validação e gera
lote auditável antes da aplicação.

## Planos

Grátis não habilita API/webhooks; Start oferece cadastros e importações básicas;
Growth habilita API e eventos essenciais; Enterprise libera BI, catálogo
completo, webhooks avançados e limites ampliados. A interface apresenta o
bloqueio como capacidade do plano e CTA de upgrade, sem detalhes técnicos.
