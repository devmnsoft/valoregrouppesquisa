# Auditoria final — adminDeleteResponse sem CORS legado

1. `adminDeleteResponse` está declarada em `functions/index.js` como export `exports.adminDeleteResponse`.
2. A Function está como `onCall({ region: 'us-central1', cors: ALLOWED_CORS_ORIGINS }, ...)`; não há implementação `onRequest` com o mesmo nome.
3. O front chama a exclusão por `app.js` → `ValoraRepository.adminDeleteResponse(responseId)` → `repository.js` → provider selecionado → `firebase-repository.js`.
4. Não existe fetch direto para `cloudfunctions.net/adminDeleteResponse` nos arquivos fonte ou no bundle validado pelos scripts.
5. Existe `callFunction('adminDeleteResponse', { responseId })` em `firebase-repository.js`.
6. `firebase-repository.js` usa o wrapper `callFunction`, baseado em callable Firebase SDK/`httpsCallable`, para acionar Functions.
7. As origens CORS oficiais em `functions/index.js` são: `https://valoragroup.mnsoft.com.br`, `https://valorateste.mnsoft.com.br`, `https://gestordepesquisa.web.app`, `https://gestordepesquisa.firebaseapp.com`, `http://localhost:<porta>` e `http://127.0.0.1:<porta>`.
8. `https://valorateste.mnsoft.com.br` está permitido.
9. `https://valoragroup.mnsoft.com.br` está permitido.
10. `localhost` e `127.0.0.1` com porta estão permitidos por regex.
11. A exclusão persiste no Firestore por soft delete em `responses/<responseId>` com `deleted`, `deletedAt`, `deletedBy`, `status: 'deleted'` e `updatedAt`.
12. A listagem e métricas filtram respostas excluídas com `isDeletedResponse`/`activeResponsesOnly`, cobrindo tabela admin, cards mobile, dashboard, métricas, relatórios, histórico e comparação por unidade.
13. Correções aplicadas: CORS oficial centralizado, callable com `HttpsError` e `req.data`, remoção de fluxo HTTP/fetch direto, facade de repositório robusta, tratamento diagnóstico para erro `internal`/CORS no app, origins no config de produção/teste e validadores dedicados.
