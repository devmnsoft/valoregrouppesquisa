# Auditoria final — camada pública legada

1. **Home pública:** renderizada por `renderHome()` em `app.js`; CTAs públicos usam links `wa.me` e pesquisa gratuita.
2. **Devolutiva pública:** renderizada por `renderPublicResultFromRoute()`/`renderResult()` em `app.js`, com bundle carregado por token.
3. **Chamadas `renderResult`/`renderImmediateResultAfterSubmit`:** `renderPublicResultFromRoute()`, `tryEnhancePublicResult()`, `handlePublicSubmitSuccess()` e fallbacks pós-submit chamam essas rotinas.
4. **Chamada `getPublicResult`:** `firebase-repository.js` em `loadPublicResultFirebase(responseId, resultToken)` chama somente a callable `getPublicResult`.
5. **Motivo do 403:** o acesso falhava quando a rota pública caía no fluxo autenticado/perfil ou quando o token de resultado estava ausente/inválido; agora a rota pública retorna antes de Auth e a Function valida apenas `responseId + resultToken` contra hash.
6. **`loadProfile` em rota pública:** `loadProfile()` segue existindo apenas no fluxo autenticado de `firebase-repository.js`; `init()` em `app.js` bypassa rotas públicas antes de aguardar Firebase Auth.
7. **“Usuário inativo”:** originava de `loadProfile()`/`authedUser()`; rotas `?survey=&token=`, `?result=&rt=` e `#acessar-resultado` não entram mais nesse caminho.
8. **`data-action` vazio:** o clique agora passa por `normalizeActionName()` e registra `lastEmptyActionClick` sem chamar ação vazia; validadores bloqueiam `data-action=""`.
9. **WhatsApp como ação interna:** botões públicos foram padronizados para `<a href="https://wa.me/5591992545353...">`; `openWhatsapp` existe apenas como fallback allowlisted.
10. **`legacy_run`:** tratado por `legacyRun()` com allowlist `LEGACY_PUBLIC_ACTIONS`.
11. **`reportResponsePdf`:** implementado em `app.js`, carrega `ValoraRepository.loadPublicResult()` e chama `ValoraPdf.createReport`.
12. **`certificatePdf`/`downloadCertificatePdf`:** mapeados em `createActions()` e usam os helpers seguros de certificado com `responseId + token`.
13. **`Valora Pulse™` em tela pública:** constantes separam `PUBLIC_PRODUCT_NAME = Valora Insight™` e `PLATFORM_NAME = Valora Pulse™`; textos públicos usam o produto.
14. **`HOME`:** textos públicos usam `Início`; validadores bloqueiam regressão.
15. **“Pesquisa gratuita da Home: diagnóstico público”:** substituído por “Pesquisa gratuita”.
16. **`Invalid date`:** mitigado por `formatPublicDate()` usado em resultado, certificado, histórico e relatório.
17. **Card branco duplicado:** “Enquadramento geral sem adoçamento” foi removido; resta `renderExecutiveSummaryCard()` como card principal petróleo.
18. **Status de e-mail:** `submitSurveyResponse` grava `sent`, `failed_non_blocking`, `not_requested`; a interface mostra mensagens honestas e detalhes de erro.
19. **Provider de e-mail:** `EMAIL_PROVIDER=http_api` é o padrão por HTTP API com `SMTP` como fallback quando configurado.
20. **Correções feitas:** bypass público de Auth, `getPublicResult` público por token, link incompleto controlado, WhatsApp oficial, clique vazio seguro, `legacy_run` allowlisted, relatório PDF, textos públicos, layout mobile, data segura, remoção de duplicidade, e-mail HTTP API com fallback SMTP, status honesto, prevenção de duplo submit e validadores obrigatórios.
