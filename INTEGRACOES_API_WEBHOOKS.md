# Integrações, API pública e Webhooks — Valora Pulse

## Visão geral
O módulo **Integrações** prepara o Valora Pulse para integrações com RH, ERP, CRM, BI, planilhas, automações e sistemas internos. A arquitetura separa dados por `companyId`, aplica escopos por API key, registra auditoria em `integrationLogs` e mantém segredos fora do frontend.

## Entidades
- `integrations`: nome, tipo, provedor, status, eventos e escopos permitidos por empresa.
- `apiKeys`: `keyHash`, `keyPrefix`, escopos, status, expiração e último uso. A chave pura é exibida apenas uma vez.
- `webhooks`: URL HTTPS, hash do segredo, eventos, status, última entrega e contador de falhas.
- `integrationLogs`: registros de criação/revogação de chave, chamadas de API, webhooks, importações e exportações.

## Criar API key
1. Acesse **Integrações** como `admin_valora` ou `empresa_admin` em plano habilitado.
2. Clique em **Criar chave**.
3. Escolha escopos como `employees:read`, `employees:write`, `surveys:read`, `responses:read`, `reports:read` ou `webhooks:manage`.
4. Copie a chave exibida uma única vez e guarde em cofre seguro.

## Endpoints preparados
A Cloud Function HTTPS `publicApi` roteia:

```http
GET /api/v1/employees
POST /api/v1/employees
PUT /api/v1/employees/{id}
GET /api/v1/surveys
GET /api/v1/responses
GET /api/v1/reports
POST /api/v1/webhooks/test
Authorization: Bearer vp_live_...
```

Todos validam hash da chave, escopo, expiração, revogação, `companyId`, rate limit e retornam JSON padronizado.

## Webhooks
Eventos suportados: `employee.created`, `employee.updated`, `survey.created`, `survey.sent`, `invitation.sent`, `invitation.failed`, `response.submitted`, `report.generated`, `action_plan.created`, `action_plan.completed`, `invoice.paid`, `subscription.updated`.

Headers enviados:

```http
X-Valora-Event: response.submitted
X-Valora-Signature: sha256=...
X-Valora-Timestamp: 1782000000000
```

A assinatura usa HMAC/sha256 sobre timestamp e payload. O payload deve ser mínimo e evitar dados pessoais desnecessários.

## Importação de funcionários
Campos CSV/Excel/JSON aceitos: `name`, `email`, `phone`, `position`, `department`, `role`, `status`, `receivesEmail`, `portalAccess`.

Regras: validar e-mail, bloquear duplicidade dentro da empresa, rejeitar perfis globais, importar somente para a própria empresa, exibir resumo e registrar log.

## Exportação BI-ready
Tipos: `employees`, `surveys`, `responses_flat`, `dimensions_summary`, `survey_summary`, `company_usage`. `responses_flat` inclui empresa, pesquisa, resposta, departamento, datas, indicadores e dimensão, respeitando anonimização.

## Limites por plano
Planos sem módulo `integrations` escondem/bloqueiam a tela. Plano Enterprise libera API pública e webhooks. Exportações manuais podem permanecer limitadas por plano.

## Segurança e LGPD
- Nunca salvar API key ou segredo de webhook puro.
- Nunca colocar credencial no frontend.
- Separar tudo por `companyId`.
- Não exportar dados de terceiros.
- Anonimizar respostas quando configurado.
- Auditar criação, revogação, importação, exportação e chamadas externas.
