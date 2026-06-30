# Mapa das Functions e jornada pública de pesquisas

Auditoria inicial realizada em `functions/index.js`, `app.js`, `firebase-repository.js`, `local-repository.js`, `scripts/build-production.js`, `package.json`, `functions/package.json`, `functions/package-lock.json` e `firestore.rules`.

## Functions públicas e operacionais

| Function | Tipo | Quem pode chamar | Exige login | Coleções usadas | Secrets | Pública | Erros/diagnostics | Papel na jornada pública | Status |
|---|---|---|---|---|---|---|---|---|---|
| `getFeaturedHomeSurvey` | `onCall` v2 | Visitante, usuário logado | Não | `surveys`, `forms`, `organizations`, `companies`, `rateLimits` | Não | Sim | Retorna `diagnostics` com `acceptedCandidates`/`rejectedCandidates`; só falha se o fallback oficial também falhar | Resolve a pesquisa grátis da home; tenta reparar candidatas e cria/repara `official_free_survey` | Corrigida |
| `debugFeaturedHomeSurvey` | `onCall` v2 | `admin_valora`, `consultor_valora` | Sim | `surveys`, `forms`, `organizations`, `companies`, `users` | Não | Não | `permission-denied`, diagnostics completos sem alterar dados | Auditoria administrativa da seleção da home | Corrigida |
| `preparePublicSurveyLink` | `onCall` v2 | `admin_valora`, `consultor_valora`, `empresa_admin`, `gestor_pesquisa` | Sim | `surveys`, `forms`, `organizations`, `companies`, `auditLogs` | Não | Não | `not-found`, `permission-denied`, `public_survey_invalid` com `rejectedReasons` | Contrato único para preparar URL pública segura sem expor `tokenHash` | Corrigida |
| `repairFeaturedHomeSurvey` | `onCall` v2 | `admin_valora`, `consultor_valora` | Sim | `surveys`, `forms`, `organizations`, `companies`, `auditLogs` | Não | Não | Usa erros de `preparePublicSurveyDocument` | Wrapper administrativo para destacar/reparar pesquisa da home | Corrigida |
| `validateSurveyLink` | `onCall` v2 | Visitante, usuário logado | Não | `surveys`, `forms`, `organizations`, `companies`, `plans`, `rateLimits`, `auditLogs` | Não | Sim | `demo_blocked`, `public_survey_invalid`, `invalid_token`, `form_not_found`, `company_not_found`, `inactive_company`, `invalid_status`, `private_visibility`, `revoked` | Valida link público preparado e retorna survey/form/company sanitizados | Corrigida |
| `submitSurveyResponse` | `onCall` v2 | Visitante, usuário logado | Não | `surveys`, `forms`, `organizations`, `companies`, `responses`, `invitations`, `emailJobs`, `rateLimits`, `auditLogs` | Não | Sim | `invalid-argument`, `failed-precondition`, `already-exists`, diagnostics vindos da validação do link | Recebe respostas públicas, gera resultado e `resultToken`; e-mail é best-effort via fila | Corrigida |
| `getPublicResult` | `onCall` v2 | Visitante, usuário logado | Não | `responses`, `rateLimits`, `auditLogs` | Não | Sim | `not-found`, `permission-denied` | Exibe resultado imediato via `responseId` + `resultToken` | Corrigida |
| `sendResultEmail` | `onCall` v2 | Público com `resultToken` válido ou usuário autenticado autorizado | Condicional | `responses`, `emailJobs`, `auditLogs` | `SMTP_PASSWORD` | Parcial | Retorna `pending_retry` em falha; não bloqueia resultado | Envio/reenvio best-effort do resultado | OK |
| `lookupCnpj` | `onCall` v2 | Visitante, usuário logado | Não | `rateLimits` e BrasilAPI externa | Não | Sim | `invalid-argument`, `unavailable`; frontend permite preenchimento manual | Preenchimento assistido de cadastro público | OK |
| `registerCompanyAccount` | `onCall` v2 | Visitante | Não | `organizations`, `companies`, `users`, Firebase Auth, `auditLogs` | Não | Sim | `invalid-argument`, `already-exists`, erros de Auth | Criação pública de conta/empresa SaaS | OK |

## Pontos frontend auditados

- `renderHome` cacheia a promise `featuredHomeSurveyPromise` e evita loop de chamadas.
- `getFeaturedHomeSurveyUrl`, `getOfficialFreeSurveyUrl` e `resolveFeaturedHomeSurveyPublic` rejeitam `survey_demo`, `empresa-exemplo` e URLs com `tokenHash`.
- O botão **Destacar como pesquisa grátis na home** chama `preparePublicSurveyLink({ surveyId, featured: true, free: true })`.
- A criação de pesquisa instantânea chama `preparePublicSurveyLink({ surveyId, featured: false, free: false })` antes de exibir/copiar link.
- `renderTakeSurvey`/`submitSurvey` usam `validateSurveyLink`/`submitSurveyResponse` no fluxo Firebase público.
- `registerCompany` é exposto em `firebase-repository.js`; `local-repository.js` mantém alias local.

## Exemplo de documento oficial válido

```json
{
  "organizations/valora-oficial": { "id": "valora-oficial", "name": "Valora Group", "status": "active", "planId": "free" },
  "companies/valora-oficial": { "id": "valora-oficial", "name": "Valora Group", "status": "active", "planId": "free" },
  "forms/form_valora_insight_oficial": { "id": "form_valora_insight_oficial", "title": "Diagnóstico Gratuito Valora Insight™", "status": "active", "questions": ["q1", "q2", "q3", "q4", "q5"] },
  "surveys/official_free_survey": {
    "id": "official_free_survey",
    "companyId": "valora-oficial",
    "organizationId": "valora-oficial",
    "formId": "form_valora_insight_oficial",
    "status": "published",
    "visibility": "public",
    "featuredOnHome": true,
    "isFeatured": true,
    "homeFeatured": true,
    "visibleOnHome": true,
    "isFree": true,
    "planId": "free",
    "allowRepeat": true,
    "showResult": true,
    "revoked": false,
    "publicToken": "<token-publico-real>",
    "token": "<mesmo-token-publico-real>",
    "tokenHash": "sha256(<token-publico-real>)",
    "expiresAt": "2099-12-31T23:59:59.000Z"
  }
}
```

URLs finais são geradas em runtime por `buildPublicSurveyUrl`: `https://valoragroup.mnsoft.com.br/?survey=official_free_survey&token=<publicToken>&org=valora-group` e, para pesquisa instantânea, `https://valoragroup.mnsoft.com.br/?survey=<surveyId>&token=<publicToken>&org=<slug-da-empresa>`.
