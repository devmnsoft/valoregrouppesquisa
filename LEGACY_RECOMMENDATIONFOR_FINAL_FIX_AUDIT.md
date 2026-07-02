# Auditoria final — correção `recommendationFor`

## Onde `recommendationFor` é chamado

Busca executada com equivalente Linux ao comando solicitado em PowerShell:

```bash
rg -n -C 3 "recommendationFor" app.js report-service.js pdf.js analytics-service.js firebase-repository.js repository.js
```

Resultado relevante: a chamada pública estava em `renderResult`, na montagem do card **Próximos passos**, como fallback de `r.level?.recommendation`. Também foram adicionadas chamadas defensivas em `calculateSurveyResult`, `normalizePublicResultViewModel` e `renderFallbackResultAfterSubmit` para garantir recomendação segura quando o backend ou o resultado persistido vier sem esse campo.

## Por que não estava definido

O bundle final continha chamada `recommendationFor(...)`, mas o arquivo legado `app.js` não declarava `function recommendationFor` em escopo global do arquivo. Em produção, isso causava `ReferenceError` no momento em que o resultado era renderizado após o envio público.

## Qual fluxo quebrava

O fluxo afetado era a jornada pública após submissão:

1. participante envia a pesquisa pública;
2. `handlePublicSubmitSuccess` recebe `responseId` e `resultToken`;
3. `renderResult` tenta montar o HTML do resultado;
4. a expressão de recomendação chama `recommendationFor(...)` quando `level.recommendation` está vazio;
5. como o helper não existia, a tela caía no erro público genérico com código `unknown`.

Esse erro também poderia impactar botões e entregáveis associados ao resultado, como WhatsApp, e-mail de resultado e certificado, porque a tela principal não terminava a renderização.

## Helper criado

Foi criada `function recommendationFor(levelOrScore, form = null, result = null)` no escopo global de `app.js`, com aliases seguros:

- `getRecommendationFor(...)`;
- `resultRecommendationFor(...)`.

Também foram expostos em `window` para diagnóstico em navegador:

- `window.recommendationFor`;
- `window.getRecommendationFor`;
- `window.resultRecommendationFor`.

Após deploy, `typeof recommendationFor` deve retornar `"function"` no console do navegador.

## Fallback criado

A recomendação agora é derivada nesta ordem:

1. texto direto em `levelOrScore` ou `result.level` (`recommendation`, `message`, `description`);
2. faixa compatível em `form.resultBands` ou `result.resultBands`;
3. fallback institucional seguro por nota normalizada:
   - nota >= 4;
   - nota >= 2.5;
   - nota abaixo de 2.5.

Além disso:

- `calculateSurveyResult` sempre retorna `level.title`, `level.label` e `level.recommendation`;
- `normalizePublicResultViewModel` consolida campos mínimos para renderização pública;
- `handlePublicSubmitSuccess` persiste o último resultado em `sessionStorage` e protege `renderResult` com `try/catch`;
- `normalizeResultRenderError` transforma `ReferenceError`/`is not defined` em código `result_render_error`;
- `renderFallbackResultAfterSubmit` mostra `Resultado registrado` com recomendação segura caso a tela completa falhe;
- `publicApiFriendlyError` não expõe a mensagem técnica crua ao usuário e usa `result_render_error`.

## Como foi testado

Validadores adicionados ao `package.json`:

```bash
npm run legacy:recommendation-helper
npm run legacy:result-no-referenceerror
npm run legacy:calculate-result-recommendation
npm run legacy:result-fallback-after-submit
npm run dist:no-undefined-result-helpers
```

Também foram previstos os comandos de verificação/build/deploy solicitados:

```bash
npm run check
npm run build:prod
firebase deploy --only hosting --project gestordepesquisa
```
