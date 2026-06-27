# Sprint 21 — Auditoria da jornada pública

## Diagnóstico objetivo

1. `dynamic` ainda existe em áreas administrativas/legadas (`AuthService`, repositórios de usuário, organização, planos e migração), mas foi removido dos services públicos e dos repositórios usados diretamente por validate/submit/result.
2. Métodos longos foram eliminados na jornada pública; validadores cobrem `Services/PublicSurveys` e `Services/PublicResults`.
3. `PublicSurveyService` e `PublicResultService` agora só orquestram.
4. Montagem de DTO foi movida para `PublicSurveyAssembler` e `PublicResultAssembler`.
5. Score foi movido para `PublicAnswerScorer` e `PublicSurveySubmitter`.
6. Validação de survey foi movida para `PublicSurveyValidator`.
7. Helpers grandes foram substituídos por classes pequenas.
8. Queries da jornada pública retornam read models tipados.
9. `Dictionary<string, object>` entra apenas no normalizador, com conversão segura.
10. Submissão pública usa uma conexão e transação no `PublicResponseTransactionService`.
11. Repositórios de submit recebem `IDbConnection`/`IDbTransaction` ou `IDbTransaction`.
12. `resultToken` é retornado apenas no submit; o banco guarda `result_token_hash`.
13. Validadores novos verificam tipagem, tamanho e fronteira transacional.
14. E2E PostgreSQL segue pendente de execução neste ambiente sem `dotnet`/Docker ativos.
15. Migração Firestore → PostgreSQL permanece documentada para dry-run e validação posterior.
16. Homologação foi documentada nos guias da sprint.

## Mapeamento dos termos solicitados

- `dynamic`: permanece fora da jornada pública refatorada; há ocorrências legadas fora do escopo direto.
- `ExpandoObject`: não usado na jornada pública.
- `Dictionary<string, object>`: usado em DTOs de entrada e normalizado por `PublicAnswerNormalizer`.
- `GetValueOrDefault`: permanece em planos.
- `new ValoraInsightCalculator`: não usado nos services públicos.
- `private static`: não há helpers privados grandes nos services públicos.
- `IsDemo`, `demo-public-token`, `pending-backfill`: bloqueados por validadores existentes.
- `metadata-ready`: usado como status de certificado, não como certificado fake.
- `result_token_hash`: persistido e nunca montado no DTO público.
- `BeginTransaction`, `Commit`, `Rollback`: centralizados em `PublicResponseTransactionService`.
- `valorapesquisa.`: schema canônico.
- `valora.`, `billing.`, `communication.`, `audit.`, `migration.`: validado por `validate-single-postgres-schema.js` para SQL canônico.
