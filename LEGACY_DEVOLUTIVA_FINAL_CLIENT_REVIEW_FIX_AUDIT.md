# Auditoria final — devolutiva pública legado

1. **Devolutiva pública:** renderizada em `renderResult`, `renderImmediateResultAfterSubmit`, `renderPublicResultFromRoute` e estados de fallback/carregamento em `app.js`.
2. **Card principal petróleo:** centralizado em `renderExecutiveSummaryCard(vm)` com classe `.result-executive-card` em `app.js`, estilizado em `style.css`.
3. **Card branco “Enquadramento geral sem adoçamento”:** não deve mais existir; o validador `scripts/validate-legacy-no-duplicate-summary-card.js` falha se o texto reaparecer.
4. **“Pesquisa gratuita da Home: diagnóstico público”:** removido da home pública; o bloco agora mostra apenas “Pesquisa gratuita”.
5. **“Valora Pulse™” em tela pública:** mantido apenas em contexto administrativo/interno; devolutiva, certificado e relatório público usam `PUBLIC_PRODUCT_NAME = 'Valora Insight™'`.
6. **“HOME”:** rótulos públicos foram trocados para “Início” quando eram navegação pública.
7. **“Invalid date”:** mitigado por `formatPublicDate(value, fallback)` em `app.js` e pelo validador `scripts/validate-legacy-no-invalid-date.js`.
8. **Botão WhatsApp:** helper único `publicWhatsappContactUrl` e helper HTML `whatsappLink` em `app.js`; ações públicas usam `openWhatsapp` ou links `wa.me/5591992545353`.
9. **“Esta ação ainda não está disponível neste ambiente”:** substituída no fallback genérico de action por mensagem amigável sem erro técnico, com tratamento especial para WhatsApp.
10. **`reportResponsePdf`:** chamado por botões `data-action="reportResponsePdf"` e mapeado em `createActions`; função implementada em `app.js`.
11. **`certificatePdf`/`downloadCertificatePdf`:** mapeados em `createActions`; ambos convergem para geração segura de certificado em `app.js`.
12. **Firebase Auth indevido em tela pública:** o fluxo de resultado público usa `ValoraRepository.loadPublicResult(responseId, resultToken)`; validador existente impede `signInWithPassword` em `firebase-repository.js`.
13. **Link de resultado:** montado com `responseId` + `resultToken` no retorno de `submitSurveyResponse` e consumido por `renderPublicResultFromRoute`/`reloadPublicResult`.
14. **Status de e-mail:** `functions/index.js` define `sent`, `failed_non_blocking`, `failed` e detalhes classificados por `classifyEmailError`; `app.js` exibe mensagem honesta via `renderEmailDeliveryStatus`.
15. **Correções feitas:** textos públicos, WhatsApp, mobile sem overflow, remoção de duplicidade do card, relatório PDF, certificado com marca pública, `legacy_run` com allowlist, action handlers, SMTP 587/TLS, status de e-mail honesto, submit público protegido contra duplicidade e validadores finais.
