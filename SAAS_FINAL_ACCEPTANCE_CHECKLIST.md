# SAAS FINAL ACCEPTANCE CHECKLIST.md

Documento atualizado na Sprint 43.

- Front oficial: `backend/Valora.Web`.
- Stack permitida: ASP.NET Core MVC/Razor Pages, Bootstrap 5, JavaScript puro, jQuery e AJAX.
- Node permitido apenas para scripts, validadores, automação, Playwright e gates.
- Swagger corrigido sem `ConflictingActionsResolver`.
- Endpoint oficial de resultado: `GET /responses/{responseId}/result`.
- Gaps operacionais restantes estão centralizados em `ASPNET_WEB_API_GAPS.md`.

## Checklist adicional — backend oficial

- [x] `backend/Valora.sln` documentado como solução oficial.
- [x] `backend-v2` documentado apenas como referência temporária.
- [x] Validador `backend:official-validate` adicionado ao `package.json`.
- [x] DTOs oficiais de consolidação não incluem `password_hash`, `token_hash` ou `result_token_hash`.
- [ ] Implementar repositories/services completos para módulos, assinatura, uso, dashboard e menu dinâmico.
- [ ] Executar `dotnet build backend/Valora.sln` e `dotnet test backend/Valora.sln` em ambiente com SDK .NET instalado.

## Sprint operacional oficial
- [x] Repositories/services SaaS oficiais declarados para módulos, assinatura, uso, entitlements, dashboard e menu.
- [x] SQL oficial para relatórios, certificados, exportações, LGPD e e-mail.
- [x] DTOs seguros sem senha/hash/token/segredo SMTP.
- [x] Web MVC/Razor com telas operacionais.
- [x] Validador `backend:reports-email-validate`.
- [ ] Build/test .NET em ambiente com SDK disponível.

## Checklist adicional — importação controlada

- [ ] Dry-run executado antes de qualquer apply.
- [ ] Conflitos bloqueantes resolvidos ou batch bloqueado.
- [ ] Relatório de divergências revisado.
- [ ] Apply confirmado por `admin_valora`.
- [ ] Rollback por batch planejado e validado.
- [ ] Cutover readiness sem bloqueadores.

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

## Release Candidate 0.9.0-rc1

Esta documentação passa a considerar o Release Candidate `0.9.0-rc1` como pacote de homologação real da versão oficial localizada em `backend/Valora.sln` e `database/postgresql`. O legado e `backend-v2` permanecem apenas como referência histórica e não fazem parte do build oficial.

Antes de produção, execute em ambiente completo: `dotnet restore backend/Valora.sln`, `dotnet build backend/Valora.sln`, `dotnet test backend/Valora.sln`, validadores npm oficiais, PostgreSQL homologação, health checks HTTP, fluxos SaaS/pesquisa/relatórios/LGPD/e-mail/importação, backup/restore descartável e scripts `tools/*/backend-prd-*`.

Não versionar `.env`, dumps, logs sensíveis, dados reais, certificados reais ou secrets. Não executar cutover produtivo automático; seguir `CUTOVER_PLAN.md`, `ROLLBACK_PLAN.md`, `BACKUP_RESTORE_RUNBOOK.md`, `RELEASE_CANDIDATE_NOTES.md` e `PILOT_USERS_HOMOLOGATION_PLAN.md`.

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

## Sprint fix domain duplicate entities

- [x] Entidades duplicadas de `Valora.Domain.Entities` consolidadas em arquivos próprios.
- [x] Validador `npm run backend:domain-entities-validate` criado para bloquear regressão de `CS0101` provável.
- [x] Teste estático em `Valora.Tests` criado para validar duplicidade de entidades por namespace.
- [ ] Rodar `dotnet restore/build/test/format` em ambiente com SDK .NET instalado.

## Sprint Web Legacy Public Parity
A migração da Web oficial ASP.NET somente será considerada completa quando houver paridade verificável de layout e jornada pública com o legado da raiz: Home comercial, diagnóstico gratuito, pesquisa pública, resultado, certificado, LGPD, WhatsApp/contato, footer, modal/toast/loading, ValoraBot visual, mobile-first e separação total entre `_PublicLayout` e `_AdminLayout`. A Web oficial deve consumir apenas a API oficial, sem Firebase, sem acesso direto ao banco, sem JSON bruto, sem dados fake e sem exposição de senha, hashes, tokens, segredos, connection strings ou payload sensível.
