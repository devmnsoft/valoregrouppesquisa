# Legacy to Firebase Regression Audit

Data: 2026-07-01

## Escopo auditado

Arquivos revisados: `app.js`, `firebase-repository.js`, `repository.js`, `api-repository.js`, `local-repository.js`, `data-normalization.js`, `config.js`, `functions/index.js`, `firestore.rules`, `firebase.json`, `package.json`, `scripts/build-production.js`, `scripts/validate-*.js` e documentos `*_AUDIT.md` existentes.

## 1. Fluxos que ainda mantêm lógica legada/local/session.store

- `local-repository.js` permanece como implementação local/legada para desenvolvimento, import/export e operação sem Firebase. Em produção, o roteamento por provider não deve selecioná-lo porque `DATA_PROVIDER` está como `firebase`.
- `repository.js` ainda preserva seleção `firebase/api/hybrid`, mas a configuração de produção mantém `ALLOW_API_PRODUCTION_CUTOVER: false`, impedindo corte automático para API externa.
- `app.js` ainda possui caches em memória (`publicSurveyCache`, `publicResultCache`, `ValoraRuntimeCache`) e helpers locais para renderização/resiliência, mas o fluxo público publicado prioriza Cloud Functions.
- `session.store`/estado local pode existir como compatibilidade histórica, porém não deve substituir retorno válido de Function para pesquisa pública, destaque da home, submissão ou resultado.

## 2. Fluxos que usam Firebase/Cloud Functions

- Pesquisa destacada na home: `firebase-repository.js` chama `getFeaturedHomeSurvey` via callable; `functions/index.js` valida candidatos, formulário, empresa, token e consistência antes de retornar.
- Preparação de link público: `preparePublicSurveyLink` usa Admin SDK, gera token público válido, hash e contrato de URL.
- Validação pública: `validateSurveyLink` carrega pesquisa/form/empresa no backend e rejeita soft delete, revogação, token inválido, formulário ausente e empresa inativa.
- Submissão pública: `submitSurveyResponse` grava `responses` via transaction Admin SDK e retorna códigos específicos em `details.code`.
- Resultado público: `getPublicResult` valida `resultToken` contra hash.
- Reparos oficiais/de destaque: `repairFeaturedHomeSurvey` e `repairOfficialFormDocument` usam Admin SDK.

## 3. Fallbacks para API externa

- `api-repository.js` e rotas ASP.NET/API permanecem no repositório para arquitetura híbrida/migração.
- Em produção Firebase, o contrato validado exige `DATA_PROVIDER: 'firebase'` e `ALLOW_API_PRODUCTION_CUTOVER: false`.
- O gate `public:submit-firebase-only` bloqueia regressão em que `submitPublicSurveyResponse`, validação ou resultado público caiam para `ValoraApiRepository`/`external-api`.

## 4. Fallbacks para Firestore direto público

- O frontend ainda carrega o Firebase SDK para área autenticada/admin.
- Fluxos públicos sensíveis não devem depender de leitura direta pública: validação, submissão, resultado e destaque passam por callable Functions.
- Rules impedem auditoria direta pelo frontend; `audit()` deve usar `logServerEvent` quando autenticado e falhar de forma não bloqueante.

## 5. Cache com risco de dados antigos

- Risco encontrado: `ValoraRuntimeCache.featuredHomeSurveyPromise/result/error`, `ValoraRuntimeDiagnostics.lastFeaturedHomeSurvey*` e `publicSurveyCache` podiam manter survey/form antigos após destaque, edição ou exclusão.
- Correção aplicada/validada: `clearFeaturedHomeSurveyCache(reason)` limpa todos esses pontos e é chamado após salvar/destacar/remover destaque, editar/excluir/arquivar pesquisas e formulários e reparos relacionados.

## 6. Fallback `official_free_survey` competindo com pesquisa real

- Risco encontrado: fallback oficial poderia ser marcado como destaque e vencer uma pesquisa real clicada.
- Correção aplicada/validada: `hasRealFeaturedSurvey()` valida destaque real com form, empresa ativa/trial, token e flags; `ensureOfficialFreeSurveyFallback({ markFeatured })` só marca oficial se não existir destaque real válido.
- `getFeaturedHomeSurvey` penaliza fallback oficial e prioriza destaque manual (`homePinned`, `featuredSource: admin_click`, `featuredBy`).

## 7. Risco de formulário/pesquisa divergente

- Risco encontrado: payload público poderia combinar pesquisa de uma origem e formulário de outra via cache/fallback.
- Correção aplicada/validada: `preparePublicSurveyDocument`, `sanitizeFeaturedPayload`, `getFeaturedHomeSurvey` e validadores exigem `form.id === survey.formId`.
- Contrato esperado inclui `consistency.selectedSurveyId`, `surveyFormId`, `returnedFormId`, `formMatchesSurvey` e `urlSurveyId` quando aplicável.

## 8. Risco de soft delete voltar após reload

- Risco encontrado: listas e fluxos públicos podiam tratar registros arquivados/removidos como ativos se só verificassem `status` simples.
- Correção aplicada/validada: helpers `isDeletedRecord`, `isActiveSurvey`, `isPublicSelectableSurvey` e `isActiveForm` centralizam filtros para `deleted`, `isDeleted`, `removed`, `archived`, status `deleted/removed/archived`, revogação e status público fechado/inativo.

## 9. Validações existentes que não rodavam no `npm run check`

- Encontrado: validadores críticos existiam, mas não eram todos parte do gate principal (`check:critical` ausente).
- Correção: criado `scripts/validate-regression-critical-gate.js` e `package.json` agora expõe/encadeia os contratos críticos: syntax, handlers, boot público, consistência home, soft delete, parâmetros públicos em rota admin, submit Firebase-only, Firestore sem undefined, auditoria sem save-loop, LGPD e contrato de URL pública.

## 10. Correções feitas nesta auditoria

- Criado `scripts/validate-regression-critical-gate.js` como gate agregador crítico.
- Atualizado `package.json` para adicionar `check:critical`, inserir o gate em `npm run check` e fazer `build:prod` depender de `check:critical` antes de gerar `dist`.
- Ajustados aliases de validação para usar os validadores específicos de handlers (`validate-action-handlers.js`), boot sem ReferenceError (`validate-public-boot-no-reference-error.js`) e dist handlers (`validate-dist-action-handlers.js`).
- Criado este documento `LEGACY_TO_FIREBASE_REGRESSION_AUDIT.md` com o mapa legado vs Firebase e os riscos residuais.

## Pontos de atenção residuais

- Deploy e testes manuais dependem de credenciais/ambiente Firebase real; devem ser executados seletivamente para Functions alteradas e Hosting, nunca deploy geral de Functions.
- `local-repository.js` e `api-repository.js` continuam necessários para migração/legado, mas devem permanecer fora do fluxo público produtivo Firebase.
- Qualquer mudança futura em renderização pública deve manter o contrato de consistência survey/form e invalidar cache após mutações.
