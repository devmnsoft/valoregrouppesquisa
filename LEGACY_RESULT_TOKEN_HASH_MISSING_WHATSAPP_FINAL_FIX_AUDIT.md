# Auditoria final — result_token_hash_missing + WhatsApp

1. `submitSurveyResponse` salva respostas em `functions/index.js`, na coleção `responses`, via `tx.set(ref,response)` dentro de transação.
2. `submitSurveyResponse` agora gera `createResultAccessPair()` e salva `resultTokenHash`, `resultTokenCreatedAt` e `resultAccessEnabled` na resposta nova.
3. O `resultToken` real é gerado por `createBearerToken()` e por `createResultAccessPair()` em `functions/index.js`.
4. O `resultTokenHash` é gerado exclusivamente por `hashBearerToken(resultToken)` e salvo no Firestore.
5. `sendResultEmailInternal` monta o link por `buildPublicResultUrl(responseId,resultToken)`.
6. `sendResultEmail` rotaciona token com `rotateResultTokenForResponse`, usa o token real retornado e nunca envia `resultTokenHash`.
7. O WhatsApp de resultado monta o link no callable `adminCreateResultShareLink`, usando `buildPublicResultUrl` após rotação.
8. O WhatsApp usa `access.resultToken` real na URL; o hash permanece apenas em `responses/<id>.resultTokenHash`.
9. `getPublicResult` retorna `result_token_hash_missing` quando `response.resultTokenHash` não existe, registrando diagnóstico em `public_result_access_errors`.
10. Respostas antigas sem `resultTokenHash` são tratadas por `rotateResultTokenForResponse` antes de reenviar e-mail, compartilhar WhatsApp ou solicitar novo link.
11. O admin pode regenerar link em `adminCreateResultShareLink` e no legado `adminRegenerateResultLink`.
12. `#admin/responses` chama `shareResultWhatsapp` pelos botões desktop/mobile com `data-action="shareResultWhatsapp"`.
13. `#admin/responses` chama reenvio por `data-action="sendResultEmail"` / `resendResultEmail`.
14. As actions necessárias são `shareResultWhatsapp`, `sendResultWhatsapp`, `whatsappResult`, `adminShareResultWhatsapp`, `sendResultEmail` e `requestNewResultLink`.
15. Correções aplicadas: fonte única de token bearer/hash, rotação para respostas antigas, URL canônica, diagnóstico de hash ausente, callable público para novo link, callable admin para WhatsApp, botões admin, validadores e proteção contra vazamento de hash em URL.
