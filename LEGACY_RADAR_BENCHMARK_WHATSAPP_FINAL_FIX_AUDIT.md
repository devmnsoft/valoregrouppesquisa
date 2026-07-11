# Auditoria final — Radar, Benchmarking e WhatsApp

1. `buildValoraInsightDevolutiva` monta a devolutiva em `app.js` e injeta `radar: buildRadar(scores.dimensionScores)`.
2. O padrão antigo chamava `radarBarHtml` dentro de `buildRadar`; foi removido. A barra visual agora é chamada apenas por `renderRadarHtml` via `renderRadarBarHtml`.
3. O HTML escapado ocorria em `renderRadarHtml`, que usava `esc(item.bar)` dentro de `<code>`.
4. `radar-html-bar` aparecia como texto porque `buildRadar` persistia a string `<span class="radar-html-bar">...` no objeto e o renderer escapava esse campo.
5. `renderRadarHtml` renderiza o radar em `app.js`, com `esc(item.name)` para dados externos e HTML interno seguro para a barra.
6. O PDF renderiza o radar por `renderRadarForPdf`/`radarBarPdfSafe` em `app.js` e `report-service.js`, usando `[########--]`.
7. O Benchmarking estrutural é montado por `buildStructuralBenchmarking` e `buildBenchmarking` em `app.js`.
8. O texto repetitivo antigo ficava em `buildStructuralBenchmarking`/`buildBenchmarking`, com frases genéricas como comparação qualitativa, sob a ótica e em comparação.
9. O link de resultado por WhatsApp público é gerado por `shareCurrentPublicResultWhatsapp`; o admin usa `shareResultWhatsapp`.
10. `shareResultWhatsapp` falhava no contexto participante quando faltava token/dataset ou quando precisava diferenciar a rota pública da rota admin. A ação pública dedicada usa `?result` e `rt` atuais.
11. `adminCreateResultShareLink` existe em `functions/index.js`, é exposto em `firebase-repository.js` e mapeado em `repository.js`.
12. `createActions` mapeia `shareCurrentPublicResultWhatsapp`, `shareResultWhatsapp`, `sendResultWhatsapp`, `whatsappResult` e `adminShareResultWhatsapp`.
13. O mobile quebrava por grid/inline width antigos em `.radar-row` e `.radar-html-bar`; o CSS final cria cards verticais e grid responsivo para benchmarking.
14. Correções aplicadas: radar sem HTML no modelo, barra HTML gerada internamente, PDF ASCII seguro, benchmarking com índices Valora/GPTW/disclaimer, WhatsApp público/admin, CSS mobile e validadores de regressão.
