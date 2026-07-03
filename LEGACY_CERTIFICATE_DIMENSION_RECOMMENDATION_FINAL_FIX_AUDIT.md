# Auditoria final — correção de `dimensionRecommendation` e certificado progressivo

## Escopo auditado

Arquivos auditados: `app.js`, `pdf.js`, `report-service.js`, `firebase-repository.js`, `repository.js`, `scripts/build-production.js` e validadores `scripts/validate-*.js`.

## 1. Onde `getCertificateScore` está definido

`getCertificateScore` está definido em `app.js`, no bloco de helpers de certificado, imediatamente antes de `sanitizeCertificateText`. A função agora aceita resultado parcial e uma dimensão opcional.

## 2. Onde `dimensionRecommendation` é chamado

`dimensionRecommendation` é chamado por `getCertificateScore` para compor a recomendação do certificado com dados vindos da dimensão ou do resultado.

## 3. Por que `dimensionRecommendation` não existia

O fluxo legado de certificado chamava `dimensionRecommendation(...)` dentro de `getCertificateScore`, mas não havia declaração `function dimensionRecommendation` nem exposição no escopo global do bundle. Em produção isso causava `ReferenceError` durante a montagem do certificado.

## 4. Onde `certificateHtml` era chamado dentro de `renderResult`

`renderResult` interpolava o HTML do certificado junto com a tela de resultado usando `certificateHtml(r)`. Esse acoplamento tornava o certificado parte obrigatória do render principal.

## 5. Por que certificado estava derrubando a tela de resultado

A cadeia `renderResult -> certificateHtml -> buildCertificateData -> buildCertificateViewModel -> getCertificateScore -> dimensionRecommendation` lançava `ReferenceError`. Como o erro ocorria durante a construção do `innerHTML`, a tela de resultado não era concluída e o usuário permanecia na mensagem “Carregando resultado seguro…”.

## 6. Qual função segura foi criada

Foi criada `safeCertificateHtml(result, survey, form, company)`. Ela chama `certificateHtml` dentro de `try/catch`, registra o erro em `window.ValoraRuntimeDiagnostics.lastCertificateRenderError` e retorna o bloco progressivo “Certificado em preparação.” quando o certificado falha.

## 7. Como resultado passa a ser renderizado antes do certificado

`renderResult` monta a seção principal do resultado e usa `safeCertificateHtml(...)` apenas como bloco opcional dentro do HTML. Se o certificado falhar, `safeCertificateHtml` devolve um fallback e não interrompe a renderização da devolutiva.

## 8. Como o erro fica salvo em RuntimeDiagnostics

Erros de renderização do certificado são salvos em `window.ValoraRuntimeDiagnostics.lastCertificateRenderError`. Erros ao carregar ou melhorar o resultado público são salvos em `lastResultLoadError` e `lastResultEnhanceError`. Erros de download ficam em `lastCertificateDownloadError`.

## 9. Como validar no build/dist que não existe helper indefinido

Executar `npm run build:prod` e depois `npm run dist:no-undefined-certificate-helpers`. O validador falha se `dist/assets/app*.js` chamar `dimensionRecommendation(` sem conter `function dimensionRecommendation`, ou se `renderResult` chamar `certificateHtml(` diretamente sem o wrapper seguro.

## 10. Como testar com resposta real

1. Responder uma pesquisa pública válida.
2. Confirmar no console que o submit retorna payload com `surveyId`, token, participante, e consentimento LGPD.
3. Verificar que o resultado aparece após o submit.
4. Executar `typeof dimensionRecommendation` no console; o retorno esperado é `"function"`.
5. Forçar erro em `certificateHtml` e confirmar que a página permanece no resultado e mostra “Certificado em preparação.”.
6. Confirmar `window.ValoraRuntimeDiagnostics.lastCertificateRenderError` preenchido.
7. Abrir a URL pública `?result=<id>&rt=<token>` e confirmar que “Carregando resultado seguro…” some por renderização normal ou fallback controlado.
8. Clicar nos botões de PDF/PNG e confirmar que falhas exibem toast de aviso sem substituir `#app`.

## Comandos de auditoria executados/adaptados no ambiente Linux

Equivalente ao `Select-String` solicitado:

```bash
rg -n "dimensionRecommendation|getCertificateScore|buildCertificateViewModel|buildCertificateData|certificateHtml|renderResult" app.js pdf.js report-service.js firebase-repository.js repository.js scripts
```

Após o build, usar:

```bash
rg -n "dimensionRecommendation|getCertificateScore|buildCertificateViewModel|buildCertificateData|certificateHtml" dist/assets/app*.js
```
