# Sprint 22 — Auditoria da Jornada Pública Tipada

Data: 2026-06-27

## Escopo auditado

Foram auditados controllers, services públicos, DTOs, contracts, read models, repositories, domínio, scripts PostgreSQL, scripts de migração, validadores Node, `api-client.js`, `api-repository.js`, `repository.js`, `app.js` e `package.json`.

## Respostas objetivas

1. **Onde ainda existe `dynamic` no backend?** Fora do miolo público tipado: `AuthService`, `PlanRepository`, `OrganizationRepository`, `MigrationRepository`, `UserRepository`, contratos correlatos e listagem administrativa de comunicações. Não há `dynamic` em `PublicSurveyService` ou `PublicResultService`.
2. **Onde ainda existe `ExpandoObject`?** Não identificado no escopo auditado.
3. **Onde ainda existe cast direto como `(Guid)survey.id`?** Não identificado na jornada pública. Casts legados existem em autenticação/usuários, fora do fluxo público.
4. **Onde ainda existe cast direto como `(string)survey.title`?** Não identificado na jornada pública.
5. **Onde ainda existe `Dictionary<string, object>` sem normalização segura?** Entradas públicas continuam aceitando dicionários nos DTOs, mas passam por `PublicAnswerNormalizer` antes de score/persistência. Há dicionários de apoio em plano/capabilities fora da jornada pública.
6. **Onde `PublicSurveyService` concentra validação, DTO, score e transação?** Não concentra mais; delega validação, montagem e submissão para componentes específicos.
7. **Onde `PublicResultService` concentra validação e montagem de resposta?** Não concentra mais; delega para `PublicResultValidator` e `PublicResultAssembler`.
8. **Onde há método com mais de 80 linhas?** O validador `scripts/validate-service-method-size.js` passa para services públicos. Métodos longos remanescentes são legados fora do recorte público.
9. **Onde há service com mais de uma responsabilidade?** Remanescente em services legados de autenticação/planos; a jornada pública foi separada por validação, montagem, normalização, score e transação.
10. **Onde repositories retornam `dynamic` em vez de read models tipados?** Fora da jornada pública: `PlanRepository`, `OrganizationRepository`, `MigrationRepository`, `UserRepository` e listagens administrativas. Repositories da jornada pública retornam read models tipados.
11. **Onde repositories abrem conexão própria durante uma transação?** Métodos transacionais da submissão recebem conexão/transação ou `IDbTransaction`; consultas fora da transação abrem conexão própria por design.
12. **Onde o `resultToken` pode vazar?** O token claro é retornado somente no submit. O hash fica em `responses.result_token_hash` e é validado no serviço de resultado.
13. **Onde o `result_token_hash` pode ser retornado ao frontend?** `ResponseReadModel` carrega o hash internamente para validação; assemblers públicos não expõem esse campo em DTO de saída.
14. **Onde os validadores ainda são superficiais?** Há validação funcional para token, status, LGPD, obrigatórias e limites, mas recomenda-se ampliar casos de plano por organização e janelas de data em testes de integração reais.
15. **Onde falta teste end-to-end real com PostgreSQL?** `scripts/validate-end-to-end-api-flow.js` existe, mas depende de API/PostgreSQL locais para executar o fluxo completo homologável.
16. **Onde falta teste de rollback transacional?** Há cobertura em `PublicSurveySubmitTests.cs`; deve ser reforçada contra banco PostgreSQL real em ambiente CI com container.
17. **Onde falta teste da migração Firestore → PostgreSQL?** Existem scripts de validação/dry-run; falta execução com export real de homologação e comparação de contagens/campos pós-import.
18. **Onde falta documentação operacional de homologação?** A documentação operacional existe em arquivos dedicados; esta auditoria consolida os riscos restantes para homologação PRD.

## Mapeamento dos padrões solicitados

- `dynamic`: removido da jornada pública; remanescente em módulos legados não públicos.
- `ExpandoObject`: não identificado.
- `Dictionary<string, object>`: presente nos contratos de entrada e normalizado por `PublicAnswerNormalizer`.
- `GetValueOrDefault`: uso em entitlement/planos, fora da submissão pública.
- `(Guid)` e `(string)`: remanescentes em autenticação/repositórios legados, não em services públicos.
- `new ValoraInsightCalculator`: permitido em testes; removido de controller e service público principal.
- `private static`: usos utilitários pontuais, sem `private static dynamic` em services públicos.
- `IsDemo`, `demo-public-token`, `pending-backfill`: não usados no fluxo oficial público validado.
- `metadata-ready`: status persistido para certificado; não hardcoded em controller.
- `result_token_hash`: restrito ao banco/read model interno e validadores de segurança.
- `BeginTransaction`, `Commit`, `Rollback`: centralizados em `PublicResponseTransactionService` e `MigrationRunner`.
- `valorapesquisa.`: schema canônico único.
- `valora.`, `billing.`, `communication.`, `audit.`, `migration.`: bloqueados pelo validador de schema único nos diretórios críticos, exceto ocorrências textuais/documentais quando não representam schema SQL executável.

## Diagnóstico

A base já estava parcialmente preparada para a Sprint 22. Esta entrega consolida a auditoria, completa read models/modelos internos faltantes e preserva Firebase, provider padrão de produção e arquitetura Bootstrap/JavaScript puro. A principal dívida residual está em código administrativo/legado fora do fluxo público e na necessidade de rodar os testes completos em ambiente com .NET SDK, Docker e PostgreSQL disponíveis.
