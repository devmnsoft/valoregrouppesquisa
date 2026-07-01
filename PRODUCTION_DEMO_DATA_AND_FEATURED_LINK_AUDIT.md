# Auditoria — bloqueio de dados demo em produção e links em destaque

## Incidente auditado
A campanha `survey_demo` aparecia na área administrativa de pesquisas/links seguros com a empresa `Empresa Exemplo` / slug `empresa-exemplo`, e ações de compartilhamento/destaque podiam reutilizar uma URL pública demo. Isso podia vir de três camadas: dados reais existentes no Firestore, seed/local fallback e caches de front-end (`publicSurveyCache` e `ValoraRuntimeCache`).

## Onde `survey_demo` entrava
- Seeds do `seedStore`/`local-repository` podiam ser usados quando a aplicação inicializava em modo local ou fallback.
- Coleções reais (`surveys`, `forms`, `organizations`, `companies`, `invitations`) podiam conter registros legados com IDs, slugs, flags ou títulos de demonstração.
- Caches de runtime podiam manter URLs antigas para o destaque da home.

## Por que aparecia como ativa
Antes da correção, os filtros de listagem e de publicação consideravam principalmente status/arquivamento. Um registro demo com `status: active`/`published`, flags de destaque e token público podia passar por renderizadores administrativos, `preparePublicSurveyLink` e `getFeaturedHomeSurvey`.

## Origem de `Empresa Exemplo`
`Empresa Exemplo` é classificada como demo por nome/título e por slug `empresa-exemplo`. Pode existir tanto em seeds/fallback local quanto em documentos Firestore de `organizations`/`companies`.

## Ação que gerava URL demo
A URL podia ser gerada por `buildSurveyLink`/`openShareModal`, por `preparePublicSurveyLink` no backend ou por seleção de destaque em `getFeaturedHomeSurvey`, caso a candidata demo fosse aceita.

## Correções por camada

### Normalização central
- `data-normalization.js` agora expõe `isDemoRecord`, `isProductionEnvironment`, `isBlockedInProduction` e `isProductionVisibleRecord`.

### Front-end/admin
- `loadSurveys`, `loadForms` e `loadCompanies` filtram demo/removidos em produção.
- `openShareModal` valida survey/form/company antes de exibir link.
- `markSurveyAsHomeFeatured` valida survey/form/company antes de chamar Function.
- O retorno de `preparePublicSurveyLink` é conferido contra a pesquisa selecionada e bloqueado se a URL contiver `survey_demo`, `empresa-exemplo`, `demo-token` ou `tokenHash=`.
- `clearFeaturedHomeSurveyCache` limpa cache de destaque e `publicSurveyCache`.

### Firebase repository/local fallback
- `firebase-repository.js` filtra surveys removidas/demo em produção e limpa caches de destaque.
- `local-repository.js` remove seeds demo em produção e impede que fallback local repopule listagens administrativas.

### Backend Functions
- `functions/index.js` tem helpers equivalentes (`isDemoRecord`, `isBlockedInProduction`, `forbiddenPublicUrl`).
- `preparePublicSurveyDocument` bloqueia survey/company/form demo, survey removida/arquivada/revogada, mismatch de formulário e URL pública proibida.
- `getFeaturedHomeSurvey` rejeita `demo_survey`, `demo_company`, `demo_form` e `demo_public_url`, mantém fallback oficial apenas quando não houver pesquisa real válida e retorna `consistency` com survey/form/company/url.
- `validateSurveyLink`/`loadValidSurvey` bloqueiam demo antes de abrir formulário público.
- `purgeProductionDemoData` arquiva/revoga dados demo de forma auditável com dry-run.

### Purga operacional
- `scripts/purge-production-demo-data.js --dry-run` é o modo padrão seguro.
- `scripts/purge-production-demo-data.js --apply` aplica arquivamento/revogação sem apagar fisicamente respostas reais.

## Garantias adicionadas
Os validadores em `scripts/validate-*.js` falham se o fluxo voltar a permitir demo em listagens, compartilhamento, destaque, URL pública, fallback local ou purga sem dry-run.
