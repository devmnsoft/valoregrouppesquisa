# Sprint 42 — Auditoria de Homologação Final

1. O Valora.Web continua sendo ASP.NET Core? **Sim**, em `backend/Valora.Web`.
2. O Valora.Web está na solution? **Sim**, validado por `backend:build`.
3. Existe front oficial paralelo indevido? **Não identificado**; Node fica restrito a scripts/testes.
4. O Valora.Web acessa banco, Dapper, EF, repositories ou Firebase diretamente? **Não**, o gate `web:no-data-access` cobre isso.
5. A configuração da API no Valora.Web está hardcoded? **Não para produção**, `/web-config.js` usa `IConfiguration`/Options/ambiente.
6. Permite trocar API_BASE_URL por ambiente? **Sim**, via `Api__BaseUrl`.
7. IIS pronto? **Sim**, com `web.config`, documentação e dry-run.
8. Docker pronto? **Sim**, `valora-web`, `valora-api`, `valora-postgres` no compose.
9. web.config correto? **Sim**, validado estruturalmente.
10. Health Web existe? **Sim**, `/health/web`.
11. Endpoint de versão existe? **Sim**, `/health/web/version`.
12. Smoke publicado Web existe? **Sim**, script Node e BAT.
13. Smoke API existe? **Sim**, em `prod:smoke`.
14. Smoke login/dashboard existe? **Sim**, Playwright e smoke publicado.
15. Smoke pesquisa pública existe? **Sim**, testes e gates existentes.
16. Smoke certificado/fallback existe? **Sim**, `web:certificate-binary` e e2e.
17. Rollback Web? **Sim**, documentado em IIS/Docker.
18. Rollback API? **Sim**, checklist existente e gates.
19. Cutover Firebase/API? **Sim**, dry-run obrigatório; produção não muda automaticamente.
20. Publicação IIS? **Sim**, `ASPNET_WEB_DEPLOYMENT_IIS.md`.
21. Publicação Docker? **Sim**, `ASPNET_WEB_DOCKER_RUNTIME.md`.
22. Checklist manual? **Sim**, `SAAS_FINAL_ACCEPTANCE_CHECKLIST.md`.
23. Limitações conhecidas? **Sim**, `KNOWN_LIMITATIONS_BEFORE_PRODUCTION.md`.
24. Monitoramento mínimo? **Sim**, `ASPNET_WEB_OBSERVABILITY.md`.
25. O que ainda impede produção? Go/no-go depende de smoke remoto, bug bash de homologação, aprovação de cutover e ausência de bugs críticos conhecidos.

## Mapeamento

Termos auditados: Valora.Web, frontend-web, server.js, React, Vue, Angular, API_BASE_URL, localhost:5080, localhost:5088, appsettings, web.config, Dockerfile, docker-compose, ValoraOfficialPage, ValoraAdminPage, connection string, Dapper, Npgsql, DbConnection, Valora.Infrastructure, Firebase, password_hash, token_hash, result_token_hash, SMTP password, private_key.
