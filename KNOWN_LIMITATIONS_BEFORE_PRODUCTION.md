# Limitações conhecidas antes da produção

- A bridge corrige a interação do menu mobile legado, mas não substitui validação live após deploy.
- Usuários podem manter HTML antigo em cache de navegador/proxy até reabrir a aplicação; `no-store` em `index.html` mitiga esse risco.
- A publicação precisa confirmar que `dist/` recebeu os arquivos raiz atualizados, pois o Firebase Hosting aponta para `dist`.
- O teste standalone prova independência do `app.js`, mas não valida autenticação real do Firebase.
- Recomenda-se smoke pós-deploy em viewport mobile, abrindo `window.ValoraAdminMobileMenuBridge.debug()` no console.
