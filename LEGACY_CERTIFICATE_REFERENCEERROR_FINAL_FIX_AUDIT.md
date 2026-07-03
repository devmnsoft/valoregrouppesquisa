# Auditoria final — ReferenceError em certificado legado

## Escopo auditado
Arquivos auditados: `app.js`, `pdf.js`, `report-service.js`, `firebase-repository.js`, `repository.js`, `scripts/build-production.js` e validadores `scripts/validate-*.js` relacionados ao fluxo legado.

## 1. Onde `dimensionRecommendation` é chamado
- `app.js`: chamado por `getCertificateScore` para produzir recomendação segura do certificado a partir de percentual, nota normalizada ou texto direto.
- Após a correção, a função está declarada no mesmo IIFE de `app.js`, antes dos helpers de certificado, e exposta em `window.dimensionRecommendation` e `window.getDimensionRecommendation`.

## 2. Onde `getCertificateScore` está definido
- `app.js`: definido na seção de helpers de certificado, depois de `safeText` e antes de `formatDecimal`.
- Foi substituído por implementação tolerante a dados parciais e que sempre chama o helper local definido.

## 3. Onde `buildCertificateViewModel` está definido
- `app.js`: definido na seção de certificado legado.
- Continua centralizando participante, emissor, pesquisa, data e score; agora depende de `getCertificateScore` seguro.

## 4. Onde `buildCertificateData` está definido
- `app.js`: definido após `buildCertificatePublicValidationUrl`.
- Continua montando layout, validação pública e aliases usados por PDF/HTML.
- A correção adicionou `safeBuildCertificateData` para chamadas opcionais/progressivas.

## 5. Onde `certificateHtml` é chamado diretamente
- Antes da correção, `renderResult` interpolava `${certificateHtml(r)}` diretamente na tela de resultado.
- Depois da correção, `renderResult` usa `${safeCertificateHtml(r,s,formForInsight,{name:companyLabel})}`; o certificado vira bloco opcional.

## 6. Onde `renderResult` chama certificado
- `app.js`: `renderResult` monta o resultado público/administrativo e insere o bloco de certificado.
- O ponto crítico foi removido: não há chamada direta a `certificateHtml` dentro de `renderResult`; o caminho passa por `safeCertificateHtml`.

## 7. Onde `viewResponse` chama `renderResult`
- `app.js`: foi criado `viewResponse(id, token)` que chama `safeRenderResultById(id, token)` dentro de `withLoading` e `try/catch`.
- O handler de event delegation agora retorna `viewResponse(...)`, permitindo que `safeRun` trate a Promise.

## 8. Onde `certificatePdf`/`certificatePng` chamam certificado
- `app.js`: `certificatePdf` carrega o resultado com `loadResultForCertificate`, monta dados via `safeBuildCertificateData` e chama `window.ValoraPdf || window.ValoraPDF`.
- `certificatePng` foi tornado não bloqueante e retorna aviso controlado enquanto a imagem permanece em preparação.
- `downloadCertificatePdf` e `downloadCertificatePng` já eram wrappers progressivos e continuam sem quebrar o resultado.

## 9. Por que `safeRun` não capturou a Promise rejeitada
- A implementação anterior executava `fn()` em `try/catch`, mas retornava Promises sem anexar `catch` no próprio `safeRun`.
- O wrapper `run` anexava um `catch` lateral, mas retornava a Promise original; isso permitia `Uncaught (in promise)` quando ações assíncronas rejeitavam.
- A correção faz `safeRun` detectar `result.then` e retornar `result.then(...).catch(...)`, normalizando a falha em `handleActionError` e retornando `false`.

## 10. Como a correção impede novo `ReferenceError`
- `dimensionRecommendation` agora existe no escopo léxico do bundle legado e também em `window`.
- `getCertificateScore` foi blindado contra dados parciais.
- `renderResult` não depende mais de `certificateHtml` direto.
- `safeCertificateHtml` e `safeBuildCertificateData` registram falhas em `window.ValoraRuntimeDiagnostics` e retornam fallback visual/dados mínimos.
- `viewResponse`, `certificatePdf`, `certificatePng` e `safeRun` tratam rejeições assíncronas, evitando `Uncaught (in promise)`.
- `renderPublicResultLoading` agora tem fallback de 6 segundos e não deixa a tela presa em “Carregando resultado seguro…”.

## Comandos de localização solicitados
Equivalente Linux executado com `rg` para os arquivos indicados, pois o ambiente atual é Bash/Linux. O comando cobre os mesmos termos do `Select-String` solicitado.
