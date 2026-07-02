# Plano de retirada gradual do legado

## Fase 1 — Legado como referência
- Legado preservado. - Nova versão em homologação. - Sem tráfego oficial.

## Fase 2 — Paralelo controlado
- Nova versão validada. - Dados comparados. - Usuários piloto.

## Fase 3 — Cutover assistido
- Tráfego principal vai para backend oficial. - Legado fica read-only.

## Fase 4 — Observação
- Monitoramento intensivo. - Correções rápidas. - Fallback possível.

## Fase 5 — Arquivamento
- Snapshot final. - Documentação. - Legado removido da operação. - Código legado mantido apenas em branch/tag, se aprovado.

Não remover legado agora.

## Release Candidate 0.9.0-rc1

Esta documentação passa a considerar o Release Candidate `0.9.0-rc1` como pacote de homologação real da versão oficial localizada em `backend/Valora.sln` e `database/postgresql`. O legado e `backend-v2` permanecem apenas como referência histórica e não fazem parte do build oficial.

Antes de produção, execute em ambiente completo: `dotnet restore backend/Valora.sln`, `dotnet build backend/Valora.sln`, `dotnet test backend/Valora.sln`, validadores npm oficiais, PostgreSQL homologação, health checks HTTP, fluxos SaaS/pesquisa/relatórios/LGPD/e-mail/importação, backup/restore descartável e scripts `tools/*/backend-prd-*`.

Não versionar `.env`, dumps, logs sensíveis, dados reais, certificados reais ou secrets. Não executar cutover produtivo automático; seguir `CUTOVER_PLAN.md`, `ROLLBACK_PLAN.md`, `BACKUP_RESTORE_RUNBOOK.md`, `RELEASE_CANDIDATE_NOTES.md` e `PILOT_USERS_HOMOLOGATION_PLAN.md`.
