# Auditoria de inicialização pública — Valora Pulse

## O que roda sem login
- `bootstrapApp`, `init`, `routeFromLocation`, `renderShell` e `renderGuard` podem rodar em modo visitante.
- Rotas públicas renderizam sem depender de `currentUser` e sempre chamam `releasePublicUi` para liberar clique, scroll e overlays.
- Home pública renderiza o shell imediatamente e carrega o diagnóstico gratuito de forma assíncrona, com cache em `window.ValoraRuntimeCache` e timeout de 5 segundos.
- Pesquisa pública valida links apenas por Function pública `validateSurveyLink`.
- Resultado público carrega apenas por Function pública `getPublicResult`.
- Cadastro, login, suporte público, LGPD/privacidade/termos e planos podem ser acessados antes de autenticar.

## O que só pode rodar logado
- Hidratação completa de estado privado via Firestore (`hydrateStore` / carregamento de organizações, usuários, formulários, pesquisas, respostas, cobranças, integrações e notificações).
- Dashboard administrativo/empresa/participante autenticado.
- Listeners privados, gravações administrativas e leituras de coleções internas.
- Perfil `users/{uid}`, permissões, claims e dados de empresa logada.

## Rotas públicas
- `/`, `#/`, `#home`.
- `#login` e `/login`.
- `#signup`, `#cadastro`, `#register`, `/cadastro`, `/register`.
- `#public-help`, `#suporte`, `#support`, `/suporte`, `/support`.
- `#lgpd`, `#privacidade`, `#termos`, `/privacidade`, `/termos`.
- Qualquer URL com `?survey=<id>&token=<token>`.
- Qualquer URL com `?result=<responseId>&rt=<resultToken>`.
- Validação pública de certificado com `?certificate=`.

## Rotas privadas
- `admin/*`.
- `empresa/*`.
- `participante/*` quando exigir portal autenticado.
- Dashboard, clientes/empresas, usuários, pesquisas administrativas, relatórios, configurações, financeiro, integrações, convites, respostas privadas, notificações e qualquer tela que leia dados protegidos.

## Chamadas Firestore proibidas sem auth
Visitantes não podem executar leituras diretas em:
- `surveys`.
- `forms`.
- `organizations` / `companies`.
- `users`.
- `clients`.
- `responses`.
- coleções administrativas como `invoices`, `actionPlans`, `notifications`, `integrations`, `apiKeys`, `webhooks`, `logs`.

## Functions públicas permitidas sem auth
- `getFeaturedHomeSurvey`.
- `validateSurveyLink`.
- `submitSurveyResponse`.
- `getPublicResult`.
- `lookupCnpj`.
- `lookupCep`.
- `createPublicSupportConversation`.
- `sendPublicSupportMessage`.
- `createPublicSupportTicket`.

## Decisões implementadas
- Timeout de Auth em 5 segundos libera modo visitante e registra warning.
- `renderGuard` prioriza `isPublicRoute` e nunca bloqueia rota pública por ausência de login.
- `renderHome` não usa fallback Firestore/estado privado para visitante; a Function de destaque é cacheada uma vez por carregamento e falha de forma controlada.
- `resolveFeaturedHomeSurveyPublic` bloqueia fallback Firestore quando `session.authUser` não existe.
- `validatePublicSurveyPublic` retorna erro controlado no modo visitante quando a Function falha; não consulta `surveys/forms/organizations` sem auth.
- Erros globais em modo visitante são registrados em `window.ValoraRuntimeDiagnostics.lastPublicBootError`, liberam a UI e não derrubam cliques.
- Build de produção injeta `window.ValoraBuildInfo`, atualiza service workers e remove caches antigos Valora/app.
