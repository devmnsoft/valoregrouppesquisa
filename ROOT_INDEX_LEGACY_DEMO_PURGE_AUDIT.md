# Auditoria — purge de demo/legado da raiz, home e produção

## Escopo auditado
Arquivos auditados: `index.html`, `dist/index.html`, `app.js`, `local-repository.js`, `firebase-repository.js`, `repository.js`, `data-normalization.js`, `config.js`, `functions/index.js`, `scripts/build-production.js`, `scripts/purge-production-demo-data.js` e `package.json`.

## Onde o projeto antigo entrava

### Index raiz e ordem de scripts
O `index.html` da raiz carrega o repositório local antes do Firebase/repository. Isso permanece aceitável porque `local-repository.js` agora executa `purgeLegacyLocalStorageKeys()` em produção e não usa `localStorage` legado quando `DATA_PROVIDER === "firebase"`.

### Local repository / seed / cache
A fonte local podia reidratar `survey_demo`, `empresa-exemplo`, nomes como `Empresa Exemplo`, tokens demo e chaves antigas (`valoraPulseFinal800`, `demo`, `antigo`, `local seed`). A correção filtra coleções sensíveis em produção e limpa chaves de localStorage sem remover sessão/Auth/Firebase.

### Firestore real
Registros demo ainda podem existir em `surveys`, `forms`, `organizations`, `companies`, `invitations` e `responses`. O backend agora bloqueia publicação/validação/home para registros classificados por `isLegacyOrDemoRecord`/`isDemoRecord`, e a callable `purgeProductionDemoData` gera relatório dry-run ou arquiva/revoga quando `apply:true`.

### Cache de front-end
URLs públicas com `survey_demo`, `empresa-exemplo`, `demo-token` ou `tokenHash` são descartadas no front e no backend antes de virar CTA, link público ou destaque.

### Dist antigo / build
O build de produção continua passando pelos validadores e agora possui validadores dedicados para impedir que seed/link demo volte à raiz ou ao `dist`.

### Fallback oficial
O fallback seguro é `official_free_survey` com organização/empresa `valora-oficial` e formulário `form_valora_insight_oficial`. Ele só é marcado como destaque quando não existe pesquisa real válida destacada.

## Correções aplicadas
- Helper central `isLegacyOrDemoRecord`, `isProductionRuntime` e `shouldBlockLegacyDemo` no front.
- Equivalente backend para bloquear demo/legado em Functions.
- `local-repository.js` filtra seed/localStore em produção e ignora localStorage legado no modo Firebase.
- `getFeaturedHomeSurvey` tenta criar/reparar fallback oficial antes de falhar.
- `ensureOfficialFreeSurveyFallback({ markFeatured, forceRepair })` garante documentos oficiais e token público válido.
- `preparePublicSurveyDocument`/`preparePublicSurveyLink` bloqueiam survey/form/company demo e URLs proibidas.
- `loadValidSurvey` bloqueia links demo em validação/submissão pública.
- Home visitante troca mensagem técnica por CTA de contato; admin recebe painel com diagnóstico, reparo e limpeza demo.
- `purgeProductionDemoData` callable exige `admin_valora`, suporta dry-run/apply e retorna relatório sem token completo.
- Script local de purge trata ausência de ADC com mensagem orientativa e exige `--confirm <project>` para `--apply`.
- Validadores dedicados foram adicionados ao `package.json`.

## Como limpar após deploy
Local com ADC:

```bash
node scripts/purge-production-demo-data.js --dry-run --project gestordepesquisa
node scripts/purge-production-demo-data.js --apply --confirm gestordepesquisa --project gestordepesquisa
```

Pelo painel/admin ou console autenticado:

```js
window.ValoraFirebaseServices.functions.httpsCallable('purgeProductionDemoData')({ dryRun: true })
window.ValoraFirebaseServices.functions.httpsCallable('purgeProductionDemoData')({ apply: true })
```
