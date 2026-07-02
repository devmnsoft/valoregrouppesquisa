# Diagnóstico inicial — Homologação, cutover e hardening do backend oficial

Data: 2026-07-02.

## 1. Estado atual do build .NET

`dotnet --version` não está disponível neste container (`dotnet: command not found`). O build real deve ser executado em estação/CI com .NET SDK 8 usando `dotnet restore backend/Valora.sln` e `dotnet build backend/Valora.sln`.

## 2. Estado atual dos testes .NET

Os testes em `backend/Valora.Tests` existem, mas não puderam ser executados neste container sem SDK .NET 8. A sprint deve manter scripts de execução local/CI e teste integrado condicionado a `VALORA_TEST_POSTGRES_CONNECTION`.

## 3. Estado atual dos validadores Node

Há validadores oficiais no `package.json`: `backend:official-validate`, `backend:reports-email-validate`, `backend:migration-import-validate` e `check:critical`. Falta o validador específico de homologação/cutover.

## 4. Estado atual dos scripts SQL

A base oficial está em `database/postgresql`, com script completo e migração de importação. O ambiente local precisava de scripts padronizados para aplicar schema em PostgreSQL descartável.

## 5. Estado atual dos endpoints de migração

`backend/Valora.Api/Controllers/MigrationController.cs` expõe status, source, batch, dry-run, apply, conflitos, conciliação, rollback e readiness. Os endpoints exigem autenticação, mas precisavam reforço operacional de auditoria e paginação/configuração.

## 6. Estado atual da Web MVC de migração

`backend/Valora.Web/Controllers/MigrationController.cs` e views MVC existem para operação assistida. Faltavam páginas operacionais explícitas para Health, Version e Checks.

## 7. Gaps da auditoria anterior

A auditoria anterior indicou que importação estava funcional como contrato, porém ainda precisava evidência real, ambiente PostgreSQL, amostras sanitizadas, scripts de backup/restore, integração fina com `audit_logs`, health checks granulares e plano de cutover/rollback.

## 8. Onde a auditoria real ainda não grava em `audit_logs`

Antes desta sprint, eventos de batch/source/dry-run/apply/conflito/conciliação/rollback/readiness eram principalmente status lógico em repositórios de migração. Não havia chamada explícita de `IAuditRepository` para todos os eventos críticos.

## 9. Onde o rollback ainda é apenas lógico/marcado

`MigrationRollbackService` marcava itens como `rolled_back`, mas não executava reversão física completa sobre todas as entidades de negócio. O plano desta sprint mantém rollback itemizado e documenta limites até homologação real validar entidades.

## 10. Onde a importação ainda não usa transação física por lote

`MigrationApplyService` fazia persistência item a item via repositórios. A transação física real por conexão PostgreSQL ainda depende da implementação concreta dos repositórios no ambiente integrado; nesta sprint são adicionados contratos/configurações/evidência de lote e rollback antes de mapping.

## 11. Riscos de performance com arquivos grandes

Leitura JSON integral em memória, ausência de limite máximo configurável, ausência de tamanho de lote configurável e falta de métricas por etapa podem causar pressão de memória e timeouts.

## 12. Riscos de deduplicação com dados reais

Legacy IDs inconsistentes, e-mails duplicados, documentos com máscara/formatação distinta e coleções sem destino podem gerar conflito bloqueante ou atualização indevida.

## 13. Riscos de cutover

Risco de DNS/proxy prematuro, congelamento incompleto do legado, divergências não resolvidas, backup não testado, comunicação insuficiente e falta de janela aprovada.

## 14. Riscos de rollback produtivo

Risco de restore em base errada, perda de evidência, divergência entre legado read-only e backend oficial, comunicação tardia e rollback parcial de batches sem reconciliação.

## 15. Plano objetivo da sprint

Criar ambiente de homologação local, scripts de backup/restore, amostras sanitizadas, testes integrados condicionais, health/operations, auditoria operacional em `audit_logs`, validadores, checklists e runbooks para homologação, cutover e rollback sem executar cutover nem tocar em dados reais.
