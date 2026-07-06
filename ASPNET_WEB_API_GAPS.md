# ASP.NET Web API Gaps

## Gaps controlados atuais
Os endpoints administrativos antigos em `WebAdminModulesController` não retornam mais dados fake como produção. Quando ainda não há repository real, retornam HTTP 501 com `WEB_ADMIN_REAL_REPOSITORY_REQUIRED`.

## Repositories pendentes
OrganizationRepository, UserRepository, FormRepository, SurveyRepository, SurveyLinkRepository, ResponseRepository, AuditRepository, SettingsRepository e UsageRepository precisam ser concluídos para produção plena.

## Regras de segurança monitoradas
- `PLAN_LIMIT_REACHED` deve ser retornado por comandos que ultrapassem limites de plano.
- `resultToken` não deve ser persistido em localStorage.
- `result_token_hash`, `password_hash` e `token_hash` não devem aparecer na UI.
- Escopo por `organizationId`, permissões por role e módulos por plano continuam obrigatórios.

## Sprint 44

O Valora.Web permanece como frontend oficial ASP.NET Core e usa `/communications/result/{responseId}/send-email`, `/admin/email-jobs`, `/admin/email-jobs/process` e `/admin/email/config/status`.

## Consolidação oficial `backend` x `backend-v2`

- A base oficial passa a ser exclusivamente `backend/Valora.sln`.
- `backend-v2` permanece apenas como referência temporária e não deve receber novas features.
- Gaps administrativos só podem permanecer se retornarem erro controlado e estiverem descritos neste documento.
- `WEB_ADMIN_REAL_REPOSITORY_REQUIRED` continua permitido apenas para endpoints ainda sem repository real, sem dados fake e com mensagem operacional segura.
- Próximas remoções de gap: módulos, assinatura, uso, dashboard e menu dinâmico com repositories/services oficiais.

## Sprint SaaS + Reports/Certificates/LGPD/E-mail
- Gaps de módulos, assinatura, uso, dashboard e menu foram endereçados com repositories/services oficiais em `backend`.
- `WEB_ADMIN_REAL_REPOSITORY_REQUIRED` permanece apenas como marcador documental/validador; nenhum endpoint novo deve retornar dados fake.
- Gaps restantes: validação em ambiente com SDK .NET e implementação futura de PDF binário real (`pdf_pending` quando aplicável).

## Gap monitorado — importação de legado

A sprint adiciona endpoints administrativos `/migration/*` com dry-run, apply controlado, conciliação, rollback e cutover readiness. Gaps restantes: homologação com base real, tuning de performance em lotes grandes, política final de retenção de snapshots e automação de cutover produtivo (não executada por design).

## Sprint homologação/cutover backend oficial (2026-07-02)

- Homologação local: copie `.env.example` para `.env`, ajuste apenas credenciais locais e execute `tools/linux/backend-hml-01-subir-postgres.sh` ou `tools\windows\backend-hml-01-subir-postgres.bat`.
- Banco PostgreSQL: aplique `database/postgresql/scriptbd_completo.sql` e migrações com `tools/linux/backend-hml-02-aplicar-banco.sh` ou `tools\windows\backend-hml-02-aplicar-banco.bat`.
- API/Web: rode `tools/linux/backend-hml-03-rodar-api.sh` e `tools/linux/backend-hml-04-rodar-web.sh` (equivalentes Windows disponíveis).
- Testes integrados: defina `VALORA_TEST_POSTGRES_CONNECTION` somente para base local/homologação descartável e execute `dotnet test backend/Valora.sln` em ambiente com .NET SDK 8.
- Migração com amostra: use `docs/migration-samples/*.json`; todo apply real exige dry-run, relatório de divergências, ausência de conflito bloqueante, confirmação explícita, batch, rollback e auditoria.
- Backup: execute `tools/linux/backend-hml-06-backup.sh` ou `tools\windows\backend-hml-06-backup.bat`; dumps ficam fora do versionamento.
- Restore: use `BACKUP_RESTORE_RUNBOOK.md`; exige `CONFIRM_RESTORE=RESTORE_LOCAL_HML` e, em produção, também `CONFIRM_PRODUCTION_RESTORE=true`.
- Health: valide `/health`, `/health/database`, `/health/migration`, `/health/email`, `/health/storage` e `/health/version`; na Web MVC use `/Operations/Health`, `/Operations/Version` e `/Operations/Checks`.
- Checklist/cutover/rollback: siga `HOMOLOGACAO_CUTOVER_CHECKLIST.md`, `CUTOVER_PLAN.md`, `ROLLBACK_PLAN.md` e `LEGACY_RETIREMENT_PLAN.md`. O cutover não é automático nesta sprint.
- Validação: execute `npm run backend:homologation-cutover-validate` junto dos validadores oficiais.

## Sprint backend oficial — reality check SQL/schema (2026-07-06)

- Base oficial mantida em `backend/Valora.sln`; `backend-v2` segue apenas como referência histórica e o legado da raiz permanece preservado.
- O schema oficial de planos é UUID em `plans.id` com chave natural `plans.code`; os seeds oficiais devem usar `ON CONFLICT (code)` e nunca gravar códigos textuais em `plans.id`.
- Os atributos comerciais legados `price_label`, `badge`, `public_subtitle`, `public_description`, `highlight_text` e `cta_label` não são colunas do schema oficial atual e não devem aparecer em INSERT/UPDATE SQL.
- `plan_limits` usa colunas estruturadas (`active_surveys`, `responses_per_month`, `users`, `managers`, `forms`, `public_links`, `email_invites_per_month`, `storage_mb`) com lookup por `plans.code` para obter `plan_id`.
- `plan_capabilities` usa `capability_code` e `enabled`; `capability_key`, `capability_level` e `capability_type` são contratos legados e permanecem bloqueados nos SQL oficiais.
- A organização Valora deve usar `organizations.plan_code` quando disponível e assinatura ativa em `subscriptions` apontando para `plans.id` resolvido por `plans.code`.
- O validador `npm run backend:sql-schema-validate` foi adicionado/confirmado como gate obrigatório para bloquear regressões de schema/seeds antes da homologação real.
- Endpoints ou telas ainda sem implementação real devem permanecer documentados como gap controlado; não é permitido retornar dados fake, JSON bruto sensível, stack trace, senha, hash, token ou secret.

## Release Candidate 0.9.0-rc2

RC2 registra a homologação real possível neste container: validadores Node oficiais executados, correção de UI sensível nas views operacionais, documentação de diagnóstico/auditoria/paridade/bugs e novo gate `npm run backend:rc2-homologation-validate`. A homologação runtime completa ainda deve ser repetida em ambiente com SDK .NET 8 e PostgreSQL/Docker disponíveis para executar `dotnet restore`, `dotnet build`, `dotnet test`, aplicação SQL idempotente, API/Web, health checks, importação e backup/restore reais.

## Endpoints existentes e consumidos pelo Valora.Web

- Autenticação, dashboard, planos, módulos, pesquisas, respostas, relatórios, certificados, LGPD, e-mail, operações e migração são consumidos pela Web MVC oficial via API oficial.

## Endpoints existentes e ainda não consumidos

- Endpoints administrativos auxiliares podem permanecer disponíveis para operação interna e automação, desde que documentados e protegidos.

## Endpoints faltantes bloqueantes

- Nenhum endpoint bloqueante novo foi identificado pela validação estática RC2; a confirmação final depende de homologação runtime com SDK .NET e PostgreSQL.

## Endpoints faltantes não bloqueantes

- Melhorias de suporte/ValoraBot e relatórios avançados podem evoluir após pilotos, sem criar frontend paralelo.

## Fallbacks temporários permitidos

- Apenas mensagens de erro amigáveis, estados vazios honestos e modo de outbox de desenvolvimento sem segredo SMTP.

## Fallbacks temporários proibidos

- Firebase na Web oficial, acesso direto ao banco pela Web, dados fake administrativos, dumps JSON sensíveis, stack trace, connection string, senha, hash ou token em UI/log.

## Sprint Web Legacy Public Parity
A migração da Web oficial ASP.NET somente será considerada completa quando houver paridade verificável de layout e jornada pública com o legado da raiz: Home comercial, diagnóstico gratuito, pesquisa pública, resultado, certificado, LGPD, WhatsApp/contato, footer, modal/toast/loading, ValoraBot visual, mobile-first e separação total entre `_PublicLayout` e `_AdminLayout`. A Web oficial deve consumir apenas a API oficial, sem Firebase, sem acesso direto ao banco, sem JSON bruto, sem dados fake e sem exposição de senha, hashes, tokens, segredos, connection strings ou payload sensível.
