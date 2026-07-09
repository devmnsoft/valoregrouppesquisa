# Auditoria final — links públicos de resultado, e-mail e token

1. `submitSurveyResponse` agora gera o par canônico com `createResultAccessPair()`.
2. `resultTokenHash` é salvo em `responses/{responseId}` com `resultTokenCreatedAt` e `resultAccessEnabled`.
3. O e-mail monta a URL exclusivamente com `buildPublicResultUrl(responseId, resultToken)`.
4. O e-mail usa o `resultToken` bruto recebido no submit ou rotacionado; não usa `resultTokenHash`.
5. `sendResultEmailInternal` recebe `resultToken` explicitamente e falha se ele estiver ausente.
6. `sendResultEmail` carrega a resposta salva, autentica quando admin, e rotaciona token antes de enviar para fluxo administrativo.
7. Não há fallback autorizado para usar `resultTokenHash` como `rt`.
8. `getPublicResult` compara token via `verifyResultToken(storedHash, providedToken)`.
9. `sha256` é centralizado por `hashBearerToken`, usado tanto no submit quanto na leitura.
10. `cleanText` não trunca tokens válidos: `normalizeBearerToken` aceita até 500 caracteres; tokens hex têm 64 caracteres.
11. A rotação ocorre apenas em fluxos explícitos: reenvio/admin, login por senha e solicitação de novo link.
12. `renderPublicResultFromRoute` chama `ValoraRepository.loadPublicResult(resultId, resultToken)`.
13. `loadPublicResult` passa `responseId` e `resultToken` para `getPublicResult`.
14. `renderPublicResultFromRoute` lê a query via `getPublicRouteParams`, incluindo `rt` e `resultToken`.
15. Links públicos de pesquisa são preparados por `preparePublicSurveyDocument/preparePublicSurveyLink`.
16. Links de pesquisa no celular expiravam quando token ausente/igual ao hash ou `expiresAt` vencido; agora `preparePublicSurveyDocument` renova o token e força expiração futura.
17. WhatsApp de pesquisa usa `preparePublicSurveyLink`; WhatsApp de resultado usa URL com `result` e `rt`.
18. Respostas antigas são reparadas por `adminRegenerateResultLink` ou `requestNewResultLink`, ambos rotacionando o hash e enviando token bruto novo.
19. Validadores criados: `validate-functions-result-token-single-source`, `validate-functions-result-email-uses-raw-token`, `validate-functions-get-public-result-token`, `validate-legacy-public-result-route-token`, `validate-legacy-email-template-result-link`, `validate-functions-request-new-result-link`, `validate-functions-survey-public-link-token`, `validate-legacy-whatsapp-share-links`, `validate-legacy-admin-result-link-regeneration`, `validate-secrets-not-committed`.
20. Correções aplicadas: fonte única de token, URL canônica, e-mail premium Valora Insight™, diagnóstico de token inválido, novo link por e-mail, regeneração admin, validação de pesquisa por hash e UX de erro pública.
