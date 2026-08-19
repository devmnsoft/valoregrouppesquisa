# Checklist de release final

- [ ] `dotnet clean`, `restore`, `build` e `test` passam em CI limpa.
- [ ] Publish Release de `Valora.Api` e `Valora.Web` passa.
- [ ] SQL estático passa e aplicação dupla no PostgreSQL isolado passa.
- [ ] JWT e demais secrets vêm do secret manager; nenhum valor demo em produção.
- [ ] Migração, backup e restauração foram ensaiados.
- [ ] Smoke autenticado confirma login, dashboard, diagnóstico, LGPD/resposta, participação, pipeline, insights, actions, reports, certificados, notificações, governança e System Health.
- [ ] Rotas Enterprise retornam bloqueio por plano/permissão, nunca 500.
- [ ] Isolamento cross-organization, agregação mínima e auditoria foram verificados.
- [ ] Logs não contêm senha, token, API key, webhook secret ou dados pessoais.
- [ ] Pendências aceitas possuem responsável, risco e prazo.
