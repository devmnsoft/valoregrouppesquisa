# Valora Backend Oficial

`backend/Valora.sln` é a base única e oficial da nova versão .NET do Valora.

## Projetos

- `Valora.Api`: API oficial.
- `Valora.Application`: DTOs, contratos e regras de aplicação.
- `Valora.Domain`: entidades e tipos de domínio.
- `Valora.Infrastructure`: Dapper, PostgreSQL, repositories, segurança e e-mail.
- `Valora.Tests`: testes oficiais.
- `Valora.Web`: front oficial ASP.NET Core MVC/Razor com Bootstrap 5, JavaScript puro e jQuery/AJAX.

## Banco

A persistência oficial usa PostgreSQL, Dapper, schema `valorapesquisa` e scripts em `database/postgresql`.

## Referência temporária

`../backend-v2` permanece preservado somente como referência histórica/técnica. Não implemente novas features nele.

## Comandos

```bash
dotnet build backend/Valora.sln
dotnet test backend/Valora.sln
npm run backend:official-validate
```

## Operacional pós-resposta
A solução oficial inclui contratos iniciais para relatórios, certificados, exportações, LGPD e e-mail. Use:

```bash
npm run backend:official-validate
npm run backend:reports-email-validate
```

SMTP real deve ser configurado por `VALORA_SMTP_HOST`, `VALORA_SMTP_PORT`, `VALORA_SMTP_SECURITY`, `VALORA_SMTP_USERNAME`, `VALORA_SMTP_PASSWORD`, `VALORA_SMTP_FROM_EMAIL` e `VALORA_SMTP_FROM_NAME`. Em desenvolvimento, a fila pode operar como `DevelopmentOutbox` sem expor senha na Web.

## Importação do legado

Use `POST /migration/batches`, `POST /migration/batches/{batchId}/dry-run`, `POST /migration/batches/{batchId}/apply`, `POST /migration/batches/{batchId}/rollback` e `GET /migration/batches/{batchId}/cutover-readiness`. Toda fonte deve ser JSON exportado; não há chamada ao Firebase real.

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
