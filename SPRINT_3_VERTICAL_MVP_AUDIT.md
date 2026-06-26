# Sprint 3 — Auditoria do MVP Vertical PostgreSQL + Jornada Pública via API

## Diagnóstico obrigatório

1. **Backend ASP.NET Core existe:** sim, em `backend/Valora.sln`, com projetos `Valora.Api`, `Valora.Application`, `Valora.Domain`, `Valora.Infrastructure` e `Valora.Tests`.
2. **PostgreSQL tem scripts SQL:** sim, em `database/postgresql/`, incluindo schemas, tabelas, seed oficial de planos e seed demo Valora Insight™.
3. **`api-client.js` existe:** sim, expondo `window.ValoraApiClient` com métodos HTTP e endpoints do MVP.
4. **`api-repository.js` existe:** sim, expondo login, cadastro, planos, validação pública, submissão, resultado e links de certificado.
5. **`DATA_PROVIDER` já é usado de verdade:** parcialmente. A configuração padrão permanece `firebase`, e a sprint adiciona suporte validável a `api` e `hybrid` para a jornada pública.
6. **`renderTakeSurvey` usa provider:** sim, a validação passa por `validatePublicSurveyLink`; não deve chamar `callPublicFunction` diretamente.
7. **`submitSurvey` usa provider:** sim, a submissão passa por `submitPublicSurveyResponse`; não deve chamar `callPublicFunction` diretamente.
8. **Fluxo de resultado público usa provider:** sim, existe contrato para carregar resultado público por API/gateway/local conforme provider.
9. **Certificado usa ViewModel único:** sim, `buildCertificateViewModel` centraliza dados antes da renderização/exportação.
10. **E-mail de resultado passa por gateway/API:** sim no frontend atual por gateway externo; no MVP PostgreSQL o backend cria `email_jobs` e registra comunicações.
11. **Partes que continuam no Firebase nesta sprint:** painéis administrativos completos, base histórica principal, relatórios complexos, ValoraBot, suporte, financeiro e dados já importados.
12. **Partes que entram no PostgreSQL nesta sprint:** planos públicos, cadastro/login básico, pesquisa pública, resposta, resultado público, metadata de certificado, job de e-mail e auditoria.

## Ocorrências mapeadas

Os termos abaixo foram mapeados com `rg -n` nos arquivos solicitados: `DATA_PROVIDER`, `API_BASE_URL`, `PUBLIC_SURVEY_VALIDATION_PROVIDER`, `PUBLIC_SUBMISSION_PROVIDER`, `RESULT_PROVIDER`, `callPublicFunction`, `firebaseCallable`, `submitSurveyResponse`, `validateSurveyLink`, `renderTakeSurvey`, `submitSurvey`, `renderResult`, `buildCertificateViewModel` e `dispatchPostSurveyCommunication`.

Principais pontos encontrados:

- `config.js` define `DATA_PROVIDER: 'firebase'`, `HYBRID_PRIMARY_PROVIDER`, `API_BASE_URL` e providers públicos.
- `runtime-capabilities.js` normaliza providers públicos e capacidades em runtime.
- `app.js` contém os pontos críticos da jornada pública: `renderTakeSurvey`, `submitSurvey`, `renderResult`, `validatePublicSurveyLink`, `submitPublicSurveyResponse`, `buildCertificateViewModel` e `dispatchPostSurveyCommunication`.
- `api-client.js` e `api-repository.js` são os adaptadores API/PostgreSQL do frontend.
- `scripts/validate-public-journey-provider.js` protege contra regressão de chamadas diretas a Cloud Functions no fluxo público.
