# Auditoria — envio público Firebase e edição administrativa

## Causa raiz

1. O fluxo público estava configurado como `auto`, com `PUBLIC_SUBMISSION_FALLBACKS` e `RESULT_FALLBACKS` incluindo Firestore e `external-api`, mesmo com `DATA_PROVIDER: 'firebase'`. Quando `submitSurveyResponse` retornava erro 400 de validação, o front tentava a API externa e acabava exibindo `provider_unavailable`, mascarando o erro real.
2. O envio público em `app.js` possuía um roteador local (`submitPublicSurveyAuto`) que podia tentar Cloud Functions, Firestore e API externa. Isso permitia escrita duplicada/indevida e causava a chamada CSP-bloqueada para `api.valoragroup.mnsoft.com.br`.
3. `functions/index.js` validava participante, LGPD e perguntas obrigatórias, mas nem todos os erros de validação expunham `details.code`, dificultando o mapeamento amigável no front.
4. A mensagem “Este formulário está vinculado...” ficava no fluxo de exclusão de formulário, mas não havia helpers claros para separar edição não destrutiva, alteração estrutural e exclusão. A correção torna a mensagem exclusiva de `deleteForm` e cria versionamento para alterações estruturais em formulários usados.
5. A edição de pesquisa precisava deixar explícito que editar chama `updateSurvey(id, payload)`, preserva campos críticos e nunca chama exclusão/recriação de formulário.

## Arquivos auditados

- `app.js`: fluxos `renderForms`, `openFormEditor`, `saveBuilder`, `deleteForm`, `cloneForm`, `openSurveyEditor`, `saveSurvey`, `saveQuickSurvey`, `markSurveyAsHomeFeatured`, `renderTakeSurvey`, `submitSurvey`, `submitPublicSurveyResponse`, `loadPublicResult`, `sendResultEmailAuto`.
- `firebase-repository.js`: `validatePublicSurvey`, novo `submitPublicSurveyResponse`, novo `loadPublicResult`, novo `sendResultEmail`.
- `api-repository.js`: contém `ValoraApiRepository.submitPublicSurveyResponse`, mantido para `DATA_PROVIDER: 'api'`, mas removido do fluxo Firebase.
- `repository.js`: roteamento principal e bloqueio de API externa para operações públicas quando `DATA_PROVIDER === 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER !== true`.
- `config.js`: providers públicos agora fixados em Cloud Functions em produção Firebase.
- `firebase.json`: CSP efetiva inclui Cloud Functions e a API externa apenas para consistência/cutover controlado.
- `functions/index.js`: `submitSurveyResponse`, `validateSurveyLink`, `getPublicResult`, `sendResultEmail`.
- `firestore.rules`: sem mudança necessária para visitante; resposta pública permanece via Cloud Function/Admin SDK.
- `package.json`: scripts de validação adicionados.
- `scripts/build-production.js`: build já limpa caches/service worker e gera `window.ValoraBuildInfo`.

## Providers usados após a correção

- Validação pública: `cloud-functions` (`validateSurveyLink`).
- Envio público: `cloud-functions` (`submitSurveyResponse`) como fonte única.
- Resultado público: `cloud-functions` (`getPublicResult`).
- E-mail de resultado: `cloud-functions` e, se necessário, job local best-effort; sem API externa em Firebase.

## Onde havia fallback externo

- `config.js`: `PUBLIC_SUBMISSION_FALLBACKS`, `RESULT_FALLBACKS` e `EMAIL_FALLBACKS` continham `external-api`.
- `app.js`: `submitPublicSurveyAuto` tentava `submitPublicSurveyViaExternalApi`; `getPublicResultAuto` priorizava API em `auto`; `sendResultEmailAuto` podia tentar API externa.
- `repository.js`: leitura em hybrid podia comparar providers; agora escrita pública não é duplicada e Firebase força repositório Firebase.

## Onde ocorria `provider_unavailable`

- `app.js`, no fim do antigo `submitPublicSurveyAuto`, após falhas em todos os providers. Isso convertia erro 400 de validação da Function em indisponibilidade genérica. O novo fluxo preserva `err.code`, `err.message`, `err.details` e mapeia mensagens específicas.

## Onde ocorria bloqueio de formulário vinculado

- `app.js`, `deleteForm(id)`. A mensagem agora só existe nesse trecho e foi alterada para “Este formulário está vinculado a pesquisas ativas. Clone ou encerre as pesquisas antes de excluir.”

## Alterações feitas

1. Produção Firebase agora usa `PUBLIC_SUBMISSION_PROVIDER`, `PUBLIC_SURVEY_VALIDATION_PROVIDER` e `RESULT_PROVIDER` como `cloud-functions`, sem fallback externo para submit/resultado.
2. `repository.js` força `submitPublicSurveyResponse`, `validatePublicSurvey`, `loadPublicResult` e `sendResultEmail` pelo Firebase em modo Firebase sem cutover.
3. `firebase-repository.js` implementa submit, resultado e e-mail via Cloud Functions, normaliza sucesso e preserva erros de validação.
4. `app.js` valida nome/e-mail, LGPD, token, surveyId e perguntas obrigatórias antes do submit; chama somente `ValoraRepository.submitPublicSurveyResponse`; mostra resultado imediatamente; e-mail é best-effort.
5. `app.js` remove fallback externo do submit público em Firebase e remove fallback de e-mail para API externa em Firebase.
6. `functions/index.js` adiciona `details.code` para participante obrigatório, LGPD e pergunta obrigatória; mantém falha de e-mail como `failed_non_blocking`.
7. `app.js` adiciona `getFormUsage` e `detectBreakingFormChanges`; edição textual de formulário vinculado é permitida, alteração estrutural oferece nova versão, exclusão vinculada permanece bloqueada.
8. `saveSurvey` passa a atualizar pesquisa existente por `ValoraRepository.updateSurvey(id, payload)`, preservando `id`, `createdAt`, `createdBy`, `responseCount`, tokens públicos, `formId` e empresa quando não alterados.
9. Validadores foram criados para travar regressões de provider, fallback externo, edição administrativa, vínculo de formulário, CSP e cache/build.
