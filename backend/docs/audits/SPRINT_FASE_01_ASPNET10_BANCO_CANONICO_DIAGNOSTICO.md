# Diagnóstico — Fase 1 ASP.NET Core 10 e Banco Canônico

- SHA inicial auditado: `7fc56b0ee1892d2159089fc73d9ca1b5fe5d2b1c`.
- Branch solicitada: `codex/fase-01-aspnet10-banco-canonico`.
- Observação de Git: a branch `main` não existia no clone local; a branch de trabalho foi criada a partir de `work` no SHA acima.
- Documentos lidos para auditoria: `REQUISITO_MIGRACAO_COMPLETA_ASPNET_CORE_10.md`, `BACKEND_OFICIAL_MIGRATION_GUIDE.md`, `ASPNET_WEB_API_GAPS.md`, `LEGACY_TO_BACKEND_PARITY_MATRIX.md`, `SPRINT_BACKEND_OFICIAL_MIGRATION_REALITY_CHECK_DIAGNOSTIC.md` e `backend/database/postgresql/script_completo.sql`.
- Projetos oficiais inspecionados: `Valora.Api`, `Valora.Application`, `Valora.Domain`, `Valora.Infrastructure`, `Valora.Web` e `Valora.Tests`.
- Estado encontrado: todos os `.csproj` oficiais apontavam para `net8.0` e usavam versões de pacote locais.
- Estado encontrado: entidades estruturais do domínio como `Organization`, `User`, `Plan`, `Subscription`, `UsageMonthly` e `Communication` estavam vazias ou com propriedades mínimas.
- Estado encontrado: `backend/database/postgresql/script_completo.sql` era o script histórico completo, mas ainda não havia `backend/database/postgresql/script_completo.sql` como bootstrap canônico idempotente.
- Estado encontrado: o ambiente de execução desta automação não possui o comando `dotnet`, impedindo validação real de restore/build/test/format localmente.
