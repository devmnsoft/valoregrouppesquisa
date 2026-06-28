# Sprint 37 — Valora.Web Área Autenticada SaaS

## Diagnóstico real
Valora.Web passou a operar como frontend administrativo API-first via jQuery AJAX e Valora.Api. O frontend legado e Firebase permanecem preservados; produção continua sem cutover automático.

## Módulos
Dashboard, Organização, Usuários, Formulários, Pesquisas, Links públicos, Respostas, Comunicações, Auditoria, Migração, Status do ambiente, Configurações e Planos/limites.

## Endpoints usados/criados
GET /me; GET/PUT /organization/current; GET /organization/current/usage; GET /organization/current/limits; CRUD mínimo de /users, /forms, /surveys; links públicos; /responses; /audit/events; GET/PUT /settings; health checks; comunicações e migração administrativas.

## Segurança e LGPD
Token fica em sessionStorage, logout limpa sessão, 401 redireciona para login, 403 é mensagem amigável, telas não exibem token, senha, stack trace, connection string ou JSON bruto.

## Gaps controlados
Edição profunda de perguntas, trilhas avançadas de auditoria, certificados binários e preferências avançadas ficam para sprint futura quando o domínio final for homologado. Bloqueia produção: sim para CRUD completo transacional e RBAC granular.

## Operação
Windows: tools/windows/70-validar-valora-web-area-autenticada.bat. Docker: docker compose build e scripts local:live. IIS: publicar backend/Valora.Web com API_BASE_URL apontando para Valora.Api e manter ALLOW_API_PRODUCTION_CUTOVER=false.
