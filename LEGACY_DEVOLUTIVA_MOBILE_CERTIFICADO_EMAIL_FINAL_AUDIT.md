# Auditoria final — devolutiva mobile, certificado, e-mail e acesso público

1. `renderResult` monta a devolutiva pública completa em `app.js`, carregando `responseId/resultToken`, normalizando o bundle público, gerando a devolutiva Valora Insight™ e renderizando ações/certificado.
2. `renderImmediateResultAfterSubmit` monta a devolutiva inicial pós-envio em `app.js`, com dados retornados por `submitSurveyResponse`, status de e-mail, botões seguros e CTA WhatsApp.
3. `certificateHtml` permanece em `app.js` para visualização administrativa/legada; a devolutiva pública usa `safeCertificateHtml` para evitar quebra da tela.
4. `certificatePdf` e `downloadCertificatePdf` ficam em `app.js`; `downloadCertificatePdf` usa `responseId/resultToken`, recarrega o resultado público e trata falhas com toast controlado.
5. O layout dos cards públicos está em `style.css`, incluindo as regras finais para `.public-result-section`, `.public-result-container`, `.result-card`, `.result-highlight`, `.result-summary-card`, `.result-dimension-grid`, `.result-dimension-card`, `.certificate-preview-card`, `.public-result-actions` e `.whatsapp-floating`.
6. `Valora Pulse™` foi mantido para plataforma/admin; a devolutiva pública e o PDF foram padronizados para `Valora Insight™`.
7. `HOME` não é usado na devolutiva pública; textos públicos devem usar `Início`.
8. `Invalid date` foi prevenido por `formatPublicDate`, usado por `brDate`/`brDay` e pelos dados seguros de certificado.
9. O botão WhatsApp é gerado por `publicWhatsappContactUrl`, com `wa.me/5591992545353` e mensagem pública padronizada.
10. O link de resultado é montado no front por `handlePublicSubmitSuccess` e no backend por `publicResultUrl(responseId,resultToken)`.
11. `resultToken` podia ficar vazio no retorno/rota; agora `handlePublicSubmitSuccess`, `publicResultUrl` e `getPublicResult` bloqueiam link incompleto.
12. `resultEmail.status` é definido em `functions/index.js` dentro de `submitSurveyResponse`, com `sent`, `failed_non_blocking` ou `not_requested`.
13. `accessPassword` é coletado em `buildPublicSurveySubmitPayload` e enviado ao backend.
14. O acesso por e-mail/senha é implementado por `renderParticipantResultAccess`, `submitParticipantResultAccess`, `ValoraRepository.getParticipantResultsByPassword` e a callable `getParticipantResultsByPassword`; não usa Firebase Auth.
15. Correções aplicadas: marca pública Valora Insight™, datas seguras, CSS mobile-first, remoção do card duplicado “Enquadramento geral sem adoçamento”, certificado com fallback seguro, WhatsApp oficial, link com token obrigatório, envio SMTP direto, status real de e-mail, acesso posterior por senha com hash e validadores de regressão.
