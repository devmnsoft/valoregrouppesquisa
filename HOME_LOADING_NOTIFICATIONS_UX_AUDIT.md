# Auditoria — Home, Loading, Notificações e UX

Data: 2026-07-02.

## Escopo auditado
- `app.js`: renderização da home pública/logada, ações, central de notificações, wrappers de loading.
- `notification-service.js`: geração, merge, listagem, leitura e dispensa de notificações.
- `logger-service.js`, `log-service.js`: logging operacional sem relação direta com duplicação visual.
- `firebase-repository.js`, `repository.js`: integração do destaque da home e persistência híbrida.
- `functions/index.js`: `getFeaturedHomeSurvey`, reparo oficial e deduplicação administrativa.
- `backend/Valora.Api`, `backend/Valora.Web`, `backend/Valora.Application`, `backend/Valora.Infrastructure`: direção .NET/PostgreSQL e referência de loading em `Valora.Web`.
- `NOTIFICACOES_E_ALERTAS.md`: diretriz de geração por backend/Admin SDK em produção.
- Views equivalentes do projeto antigo/`Valora.Web`: dashboard com loading e gap controlado.

## Achados
1. A mensagem `Pesquisa em destaque inválida` aparecia no bloco administrativo da home em `app.js`, acoplada à ausência de URL válida do destaque.
2. O trecho já estava limitado a `admin_valora` e `consultor_valora`, mas ficava na home principal e sem painel expansível.
3. Diagnóstico técnico deve aparecer somente para Admin Valora/consultor, dentro de `Detalhes técnicos`.
4. Ações assíncronas sem feedback uniforme: diagnóstico/reparo da home, notificações, CRUDs de formulários/pesquisas, salvar configurações, suporte e links públicos.
5. Botões com risco de duplo clique: excluir formulário/pesquisa, destacar/remover destaque, compartilhar/renovar link, enviar resposta pública, salvar settings, ações de notificação e reparos administrativos.
6. Notificações são geradas em `notification-service.js` por `generateNotifications`, `generateCompanyNotifications` e `generateGlobalNotifications`.
7. Notificações são persistidas no estado legado (`state.notifications`) e futuramente devem ir para backend/Admin SDK; a Function `dedupeNotifications` trata a coleção Firestore `notifications`.
8. Duplicação ocorria porque `listForUser` chamava `generateNotifications` a cada render e o `merge` consolidava por `id`, não por chave estável de tipo/empresa/entidade/rota, além de não respeitar suppression após dispensa.
9. Referência visual do projeto antigo: `backend/Valora.Web/wwwroot/js/pages/dashboard-page.js`, que usa `Loading.show/hide`, cards de dashboard e mensagens controladas.
10. Ajustes feitos: fallback amigável na home, painel técnico expansível, sistema global de loading, bloqueio de ações pendentes, loading em ações de notificação, dedupe/suppression no serviço, bloqueio de geração automática no front em produção, Function e script de limpeza, e plano .NET/PostgreSQL.

## Plano .NET/PostgreSQL para notificações

### Serviço
`NotificationService` em `backend/Valora.Application` deve expor:
- `GenerateOperationalNotificationsAsync`
- `ListForUserAsync`
- `MarkAsReadAsync`
- `DismissAsync`
- `DedupeAsync`

### Repositório
`NotificationRepository` em `backend/Valora.Infrastructure` usando PostgreSQL.

### Tabela `notifications`
Campos: `id`, `organization_id`, `user_id`, `role`, `audience`, `type`, `severity`, `title`, `message`, `entity`, `entity_id`, `action_label`, `action_route`, `read`, `read_at`, `dismissed`, `dismissed_at`, `email_sent`, `email_sent_at`, `created_at`, `updated_at`, `expires_at`, `dedupe_key`, `occurrence_count`, `first_seen_at`, `last_seen_at`, `suppressed_until`, `condition_hash`, `duplicate_of`, `duplicated`.

### Endpoints
- `GET /notifications`
- `PATCH /notifications/{id}/read`
- `PATCH /notifications/{id}/dismiss`
- `POST /admin/notifications/dedupe`
- `POST /admin/notifications/generate`

Até a migração terminar, o front legado não gera notificações automaticamente em produção.


## Validação executada em 2026-07-02
- Build de produção gerado: `app.fdabdba9e030.js` e `style.47a7893dbecb.css`.
- Deploy seletivo solicitado: `firebase deploy --only functions:dedupeNotifications,getFeaturedHomeSurvey,repairOfficialFormDocument --project gestordepesquisa`.
- Deploy de hosting solicitado: `firebase deploy --only hosting --project gestordepesquisa`.
- Observação: o ambiente local não possui Firebase CLI (`firebase: command not found`), então os comandos de deploy foram documentados e não executados neste container.
